import sys
import os

REQUIRED_TERMS = ["ParticleSystem", "CameraShake", "AudioSource", "Coyote", "NearMiss"]

def verify_polish(scripts_dir):
    """
    Parses and checks generated C# files for necessary premium polish declarations.
    """
    print(f"[Verifier] Parsing source directory: {scripts_dir}")
    merged_source = ""
    for root, _, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith(".cs") and "Tests" not in root:
                try:
                    with open(os.path.join(root, file), "r", encoding="utf-8") as f:
                        merged_source += f.read()
                except Exception:
                    pass

    missing = [term for term in REQUIRED_TERMS if term not in merged_source]
    if missing:
        print(f"[Verifier] FAIL: Missing premium polish elements: {missing}", file=sys.stderr)
        sys.exit(1)
    else:
        print("[Verifier] PASS: All required premium particles, sfx, and cameras are declared.")
        sys.exit(0)

if __name__ == "__main__":
    if len(sys.argv) > 1:
        verify_polish(sys.argv[1])
    else:
        print("Usage: python3 polish_verifier.py <directory>")