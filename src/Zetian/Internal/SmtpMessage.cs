using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Internal
{
    internal class SmtpMessage : ISmtpMessage
    {
        private readonly byte[] _rawData;
        private Dictionary<string, List<string>>? _headers; // Lazy init
        private bool _parsed;
        private bool _headersParsed;

        public SmtpMessage(string sessionId, string? from, IEnumerable<string> recipients, byte[] rawData)
        {
            Id = Guid.NewGuid().ToString("N");
            SessionId = sessionId;
            _rawData = rawData ?? throw new ArgumentNullException(nameof(rawData));
            Size = _rawData.Length;

            // Parse sender
            if (!string.IsNullOrWhiteSpace(from))
            {
                try
                {
                    From = new MailAddress(from);
                }
                catch
                {
                    // Invalid address
                }
            }

            // Parse recipients - optimized for ArraySegment
            if (recipients != null)
            {
                // Check if it's already an array or ArraySegment to avoid allocation
                int recipientCount;
                IList<string> recipientList;

                if (recipients is ArraySegment<string> segment)
                {
                    recipientCount = segment.Count;
                    recipientList = segment;
                }
                else if (recipients is string[] array)
                {
                    recipientCount = array.Length;
                    recipientList = array;
                }
                else if (recipients is ICollection<string> collection)
                {
                    recipientCount = collection.Count;
                    recipientList = collection as IList<string> ?? [.. recipients];
                }
                else
                {
                    string[] temp = [.. recipients];
                    recipientCount = temp.Length;
                    recipientList = temp;
                }

                MailAddress[] validRecipients = new MailAddress[recipientCount];
                int count = 0;

                for (int i = 0; i < recipientCount; i++)
                {
                    try
                    {
                        validRecipients[count++] = new MailAddress(recipientList[i]);
                    }
                    catch
                    {
                        // Invalid address
                    }
                }

                if (count < recipientCount)
                {
                    Array.Resize(ref validRecipients, count);
                }
                Recipients = validRecipients;
            }
            else
            {
                Recipients = [];
            }
        }

        public string Id { get; }
        public string SessionId { get; }
        public MailAddress? From { get; }
        public IReadOnlyList<MailAddress> Recipients { get; }
        public long Size { get; }

        public string? Subject => GetHeader("Subject");
        public string? TextBody
        {
            get
            {
                EnsureParsed();
                return field;
            }

            private set;
        }
        public string? HtmlBody
        {
            get
            {
                EnsureParsed();
                return field;
            }

            private set;
        }
        public bool HasAttachments { get; private set; }
        public int AttachmentCount { get; private set; }
        public DateTime? Date
        {
            get
            {
                string? dateHeader = GetHeader("Date");
                if (dateHeader != null && DateTime.TryParse(dateHeader, out DateTime date))
                {
                    return date;
                }

                return null;
            }
        }
        public MailPriority Priority
        {
            get
            {
                string? priority = GetHeader("X-Priority") ?? GetHeader("Priority");
                if (priority != null)
                {
                    if (priority.Contains('1') || priority.Contains("High", StringComparison.OrdinalIgnoreCase))
                    {
                        return MailPriority.High;
                    }

                    if (priority.Contains('5') || priority.Contains("Low", StringComparison.OrdinalIgnoreCase))
                    {
                        return MailPriority.Low;
                    }
                }
                return MailPriority.Normal;
            }
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                if (field == null)
                {
                    EnsureHeadersParsed();
                    if (_headers != null)
                    {
                        Dictionary<string, string> result = new(_headers.Count, StringComparer.OrdinalIgnoreCase);
                        foreach (KeyValuePair<string, List<string>> kvp in _headers)
                        {
                            result[kvp.Key] = string.Join(", ", kvp.Value);
                        }
                        field = result;
                    }
                    else
                    {
                        field = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                return field;
            }

            private set;
        }

        public Stream GetRawDataStream()
        {
            return new MemoryStream(_rawData, false);
        }

        public byte[] GetRawData()
        {
            return _rawData;
        }

        public Task<byte[]> GetRawDataAsync()
        {
            return Task.FromResult(GetRawData());
        }

        public string? GetHeader(string name)
        {
            EnsureHeadersParsed();
            if (_headers != null && _headers.TryGetValue(name, out List<string>? values) && values.Count > 0)
            {
                return values[0];
            }

            return null;
        }

        public IEnumerable<string> GetHeaders(string name)
        {
            EnsureHeadersParsed();
            if (_headers != null && _headers.TryGetValue(name, out List<string>? values))
            {
                return values;
            }

            return [];
        }

        public void SaveToFile(string path)
        {
            File.WriteAllBytes(path, _rawData);
        }

        public async Task SaveToFileAsync(string path)
        {
            await File.WriteAllBytesAsync(path, _rawData).ConfigureAwait(false);
        }

        public void SaveToStream(Stream stream)
        {
            stream.Write(_rawData, 0, _rawData.Length);
        }

        public async Task SaveToStreamAsync(Stream stream)
        {
            await stream.WriteAsync(_rawData).ConfigureAwait(false);
        }

        private void EnsureHeadersParsed()
        {
            if (_headersParsed)
            {
                return;
            }
            _headersParsed = true;
            ParseHeaders();
        }

        private void ParseHeaders()
        {
            _headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            using MemoryStream stream = new(_rawData);
            using StreamReader reader = new(stream, Encoding.ASCII);

            string? line;
            string? currentHeader = null;
            StringBuilder currentValue = new();

            while ((line = reader.ReadLine()) != null)
            {
                // Empty line marks end of headers
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                // Continuation of previous header
                if (line.StartsWith(' ') || line.StartsWith('\t'))
                {
                    if (currentHeader != null)
                    {
                        currentValue.Append(' ');
                        currentValue.Append(line.Trim());
                    }
                }
                else
                {
                    // Save previous header
                    if (currentHeader != null)
                    {
                        AddHeader(currentHeader, currentValue.ToString());
                    }

                    // Parse new header
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        currentHeader = line[..colonIndex].Trim();
                        currentValue.Clear();
                        currentValue.Append(line[(colonIndex + 1)..].Trim());
                    }
                    else
                    {
                        currentHeader = null;
                    }
                }
            }

            // Save last header
            if (currentHeader != null)
            {
                AddHeader(currentHeader, currentValue.ToString());
            }
        }

        private void AddHeader(string name, string value)
        {
            _headers ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (!_headers.TryGetValue(name, out List<string>? values))
            {
                values = [];
                _headers[name] = values;
            }
            values.Add(value);
        }

        private void EnsureParsed()
        {
            if (_parsed)
            {
                return;
            }

            _parsed = true;
            ParseBody();
        }

        private void ParseBody()
        {
            string contentType = GetHeader("Content-Type") ?? "text/plain";
            string? boundary = ExtractBoundary(contentType);

            using MemoryStream stream = new(_rawData);
            using StreamReader reader = new(stream, Encoding.ASCII);

            // Skip headers
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
            }

            string bodyContent = reader.ReadToEnd();

            if (boundary != null)
            {
                // Multipart message
                ParseMultipartBody(bodyContent, boundary);
            }
            else if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                HtmlBody = bodyContent;
            }
            else
            {
                TextBody = bodyContent;
            }
        }

        private void ParseMultipartBody(string body, string boundary)
        {
            string[] parts = body.Split(["--" + boundary], StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (part.Trim() == "--")
                {
                    break;
                }

                using StringReader partReader = new(part);
                Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
                string? line;

                // Read part headers
                while ((line = partReader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }

                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string name = line[..colonIndex].Trim();
                        string value = line[(colonIndex + 1)..].Trim();
                        headers[name] = value;
                    }
                }

                string partContent = partReader.ReadToEnd();

                if (headers.TryGetValue("Content-Type", out string? contentType))
                {
                    if (contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        TextBody = partContent;
                    }
                    else if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                    {
                        HtmlBody = partContent;
                    }
                    else if (headers.TryGetValue("Content-Disposition", out string? disposition) &&
                             disposition.Contains("attachment", StringComparison.OrdinalIgnoreCase))
                    {
                        HasAttachments = true;
                        AttachmentCount++;
                    }
                }
            }
        }

        private string? ExtractBoundary(string contentType)
        {
            Match match = Regex.Match(contentType, @"boundary=""?([^""\s;]+)""?", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}