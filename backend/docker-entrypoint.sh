#!/bin/sh
set -e

# Create upload subdirectories with proper permissions
mkdir -p /app/uploads/avatars
mkdir -p /app/uploads/notes

# Ensure proper ownership (if running as root, change to flowly user)
if [ "$(id -u)" = "0" ]; then
    chown -R flowly:flowly /app/uploads
    # Switch to flowly user and run the application
    exec gosu flowly dotnet Flowly.Api.dll
else
    # Already running as flowly user
    exec dotnet Flowly.Api.dll
fi
