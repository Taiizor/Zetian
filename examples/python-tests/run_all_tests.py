"""
Master Test Runner for Zetian SMTP Server Examples
Run all test scripts or select specific ones
"""
import os
import sys
import importlib.util
import time
from pathlib import Path

def load_and_run_test(test_file):
    """Dynamically load and run a test module"""
    module_name = Path(test_file).stem
    
    try:
        # Load the module
        spec = importlib.util.spec_from_file_location(module_name, test_file)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        
        # Run the main function if it exists
        if hasattr(module, 'main'):
            module.main()
            return True
        else:
            print(f"⚠️ No main() function in {test_file}")
            return False
    except Exception as e:
        print(f"❌ Error running {test_file}: {e}")
        return False

def display_menu():
    """Display test selection menu"""
    print("=" * 60)
    print("Zetian SMTP Server - Test Runner")
    print("=" * 60)
    print()
    print("Available test suites:")
    print("1. Basic SMTP Server Tests")
    print("2. Authenticated SMTP Server Tests")
    print("3. Secure SMTP Server Tests (TLS/SSL)")
    print("4. Rate Limited SMTP Server Tests")
    print("5. Message Storage Tests")
    print("6. Custom Processing Tests")
    print("7. Full Featured Server Tests")
    print("8. Protocol-Level Filtering Tests")
    print()
    print("9. Run ALL Tests")
    print("0. Exit")
    print()
    
    return input("Select test suite (0-9): ")

def run_single_test(test_number):
    """Run a single test suite"""
    test_map = {
        "1": "BasicExample.py",
        "2": "AuthenticatedExample.py",
        "3": "SecureExample.py",
        "4": "RateLimitedExample.py",
        "5": "MessageStorageExample.py",
        "6": "CustomProcessingExample.py",
        "7": "FullFeaturedExample.py",
        "8": "ProtocolLevelFilteringExample.py"
    }
    
    if test_number in test_map:
        test_file = test_map[test_number]
        print(f"\n{'=' * 60}")
        print(f"Running: {test_file}")
        print(f"{'=' * 60}\n")
        
        if os.path.exists(test_file):
            return load_and_run_test(test_file)
        else:
            print(f"❌ Test file not found: {test_file}")
            return False
    else:
        print("❌ Invalid test number")
        return False

def run_all_tests():
    """Run all test suites in sequence"""
    test_files = [
        "BasicExample.py",
        "AuthenticatedExample.py",
        "SecureExample.py",
        "RateLimitedExample.py",
        "MessageStorageExample.py",
        "CustomProcessingExample.py",
        "FullFeaturedExample.py",
        "ProtocolLevelFilteringExample.py"
    ]
    
    results = {}
    total_tests = len(test_files)
    passed_tests = 0
    
    print("\n" + "=" * 60)
    print("Running ALL Test Suites")
    print("=" * 60)
    
    for i, test_file in enumerate(test_files, 1):
        print(f"\n[{i}/{total_tests}] Running: {test_file}")
        print("-" * 40)
        
        if os.path.exists(test_file):
            success = load_and_run_test(test_file)
            results[test_file] = success
            if success:
                passed_tests += 1
            
            # Pause between tests
            if i < total_tests:
                print("\nPausing 2 seconds before next test...")
                time.sleep(2)
        else:
            print(f"⚠️ Test file not found: {test_file}")
            results[test_file] = False
    
    # Summary
    print("\n" + "=" * 60)
    print("TEST SUITE SUMMARY")
    print("=" * 60)
    print()
    
    for test_file, success in results.items():
        status = "✅ PASSED" if success else "❌ FAILED"
        print(f"{test_file:<35} {status}")
    
    print()
    print(f"Total: {passed_tests}/{total_tests} test suites completed")
    
    if passed_tests == total_tests:
        print("\n🏆 ALL TESTS COMPLETED SUCCESSFULLY! 🏆")
    elif passed_tests >= total_tests * 0.8:
        print("\n✅ Most tests completed successfully")
    else:
        print("\n⚠️ Some tests need attention")
    
    return results

def check_smtp_server():
    """Check if SMTP server is running"""
    import socket
    
    ports_to_check = [25, 587, 465]
    available_ports = []
    
    for port in ports_to_check:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(1)
        result = sock.connect_ex(('localhost', port))
        sock.close()
        
        if result == 0:
            available_ports.append(port)
    
    if not available_ports:
        print("⚠️ WARNING: No SMTP server detected on standard ports (25, 587, 465)")
        print("   Make sure the Zetian SMTP server is running before testing")
        response = input("\nContinue anyway? (y/n): ")
        return response.lower() == 'y'
    else:
        print(f"✅ SMTP server detected on port(s): {', '.join(map(str, available_ports))}")
        return True

def main():
    """Main test runner"""
    print("=" * 60)
    print("Zetian SMTP Server Test Suite")
    print("=" * 60)
    print()
    
    # Check if server is running
    if not check_smtp_server():
        print("Exiting...")
        return
    
    print()
    
    # Interactive mode or command line arguments
    if len(sys.argv) > 1:
        # Command line mode
        if sys.argv[1] == "all":
            run_all_tests()
        elif sys.argv[1].isdigit():
            run_single_test(sys.argv[1])
        else:
            print("Usage:")
            print("  python run_all_tests.py          # Interactive mode")
            print("  python run_all_tests.py all      # Run all tests")
            print("  python run_all_tests.py [1-8]    # Run specific test")
    else:
        # Interactive mode
        while True:
            choice = display_menu()
            
            if choice == "0":
                print("Exiting...")
                break
            elif choice == "9":
                run_all_tests()
                input("\nPress Enter to continue...")
            elif choice in "12345678":
                run_single_test(choice)
                input("\nPress Enter to continue...")
            else:
                print("❌ Invalid choice. Please select 0-9")
                time.sleep(1)

if __name__ == "__main__":
    # Change to the directory containing the test files
    script_dir = Path(__file__).parent
    os.chdir(script_dir)
    
    main()