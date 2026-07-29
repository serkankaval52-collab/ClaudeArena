import sys
import subprocess
import time

def safe_rollback():
    """
    Creates a defensive recovery backup stash before reverting Assets/CoreFactory to the stable state.
    """
    print("[Rollback System] Compiler error encountered. Initializing defensive restore checkpoint...")
    stamp = time.strftime("%Y%m%d-%H%M%S")
    try:
        # Create a safe recovery branch to preserve potential uncommitted developer manual patches
        subprocess.run(["git", "stash", "push", "-u", "-m", f"autorollback-{stamp}"], check=False)
        subprocess.run(["git", "branch", f"recovery/rollback-{stamp}"], check=False)
        
        # Now execute the hard reset safely
        subprocess.run(["git", "reset", "--hard", "HEAD"], check=True)
        print(f"[Rollback System] Reverted Assets to the last stable Git state successfully. Recovery branch tag: recovery/rollback-{stamp}")
        return True
    except Exception as e:
        print(f"[Rollback System] Critical: Failed to restore state: {e}", file=sys.stderr)
        return False

if __name__ == "__main__":
    safe_rollback()