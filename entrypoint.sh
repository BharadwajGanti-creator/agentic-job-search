#!/bin/sh
set -e
if [ -n "$RESUME_PROFILE_JSON" ]; then
  printf '%s' "$RESUME_PROFILE_JSON" > resume_profile.json
fi
exec dotnet JobSearchAgent.dll
