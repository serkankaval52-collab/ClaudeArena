#!/usr/bin/env python3
"""Create a recoverable snapshot before restoring a Git worktree to HEAD."""

import argparse
from datetime import datetime, timezone
import subprocess
import sys


def run_git(*args):
    """Run Git without raising so callers can gate every destructive step."""
    return subprocess.run(
        ["git", *args],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )


def print_failure(action, result):
    sys.stdout.flush()
    print(f"[Rollback System] ERROR: {action} failed (exit {result.returncode}).", file=sys.stderr)
    if result.stdout.strip():
        print(result.stdout.strip(), file=sys.stderr)
    if result.stderr.strip():
        print(result.stderr.strip(), file=sys.stderr)


def resolve_optional_ref(ref_name):
    result = run_git("rev-parse", "--verify", "-q", ref_name)
    return result.stdout.strip() if result.returncode == 0 else None


def safe_rollback(dry_run=False):
    """Snapshot tracked and untracked changes, then restore tracked files to HEAD."""
    repository = run_git("rev-parse", "--show-toplevel")
    if repository.returncode != 0:
        print_failure("repository validation", repository)
        return False

    head = run_git("rev-parse", "--verify", "HEAD")
    if head.returncode != 0:
        print_failure("HEAD resolution", head)
        return False

    status = run_git("status", "--porcelain", "--untracked-files=all")
    if status.returncode != 0:
        print_failure("working-tree inspection", status)
        return False

    repository_path = repository.stdout.strip()
    head_sha = head.stdout.strip()
    has_changes = bool(status.stdout.strip())
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S-%f")
    recovery_branch = f"recovery/rollback-{stamp}"
    stash_message = f"autorollback-{stamp}"

    print(f"[Rollback System] Repository: {repository_path}")
    print(f"[Rollback System] Restore target: current HEAD {head_sha}")

    if dry_run:
        print("[Rollback System] DRY RUN: no repository state will be changed.")
        print(f"[Rollback System] Would run: git stash push -u -m {stash_message}")
        if has_changes:
            print(
                "[Rollback System] Would create "
                f"{recovery_branch} at the new stash commit."
            )
        else:
            print("[Rollback System] Working tree is clean; no recovery branch should be needed.")
        print(f"[Rollback System] Would run: git reset --hard {head_sha}")
        return True

    stash_before = resolve_optional_ref("refs/stash")
    stash = run_git("stash", "push", "-u", "-m", stash_message)
    if stash.returncode != 0:
        print_failure("recovery stash creation", stash)
        print("[Rollback System] Reset was NOT run.", file=sys.stderr)
        return False

    if stash.stdout.strip():
        print(stash.stdout.strip())
    if stash.stderr.strip():
        print(stash.stderr.strip())

    stash_after = resolve_optional_ref("refs/stash")
    stash_created = stash_after is not None and stash_after != stash_before

    if has_changes and not stash_created:
        print(
            "[Rollback System] ERROR: Git reported success but no new stash commit was created; "
            "reset was NOT run.",
            file=sys.stderr,
        )
        return False

    if stash_created:
        branch = run_git("branch", recovery_branch, stash_after)
        if branch.returncode != 0:
            print_failure(
                f"recovery branch creation at stash commit {stash_after}",
                branch,
            )
            print("[Rollback System] Reset was NOT run; the stash remains at refs/stash.", file=sys.stderr)
            return False

        branch_sha = resolve_optional_ref(f"refs/heads/{recovery_branch}")
        if branch_sha != stash_after:
            print(
                "[Rollback System] ERROR: Recovery branch verification failed; "
                "reset was NOT run. The stash remains available at refs/stash.",
                file=sys.stderr,
            )
            return False

        print(f"[Rollback System] Recovery stash commit: {stash_after} (stash@{{0}})")
        print(f"[Rollback System] Recovery branch: {recovery_branch} -> {stash_after}")
    else:
        print("[Rollback System] Working tree was clean; Git created no stash entry.")

    reset = run_git("reset", "--hard", head_sha)
    if reset.returncode != 0:
        print_failure(f"working-tree reset to {head_sha}", reset)
        return False

    if reset.stdout.strip():
        print(reset.stdout.strip())
    if reset.stderr.strip():
        print(reset.stderr.strip())

    print(f"[Rollback System] Working tree restored to current HEAD commit {head_sha}.")
    if stash_created:
        print(
            "[Rollback System] Local changes remain recoverable from "
            f"{recovery_branch} and stash@{{0}}."
        )
    else:
        print("[Rollback System] No local changes required a recovery snapshot.")
    return True


def parse_args(argv=None):
    parser = argparse.ArgumentParser(
        description="Snapshot local changes and restore tracked files to the current HEAD commit."
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show the guarded operations without changing repository state.",
    )
    return parser.parse_args(argv)


def main(argv=None):
    args = parse_args(argv)
    return 0 if safe_rollback(dry_run=args.dry_run) else 1


if __name__ == "__main__":
    sys.exit(main())
