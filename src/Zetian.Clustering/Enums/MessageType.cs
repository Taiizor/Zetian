namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Message types for cluster communication
    /// </summary>
    public enum MessageType
    {
        Heartbeat,
        Join,
        Leave,
        SessionReplicate,
        SessionRemove,
        SessionMigrate,
        StateReplicate,
        ConfigurationUpdate,
        HealthCheck,
        LeaderElection,
        DataSync,
        Acknowledgment,
        Error
    }
}