#!/bin/bash
# validate-env.sh
# Validates required environment variables before starting production deployment
# Usage: ./scripts/validate-env.sh .env.production

set -e

ENV_FILE="${1:-.env.production}"
ERRORS=0

echo "🔍 Validating environment configuration: $ENV_FILE"
echo "================================================"

# Check if env file exists
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ ERROR: Environment file '$ENV_FILE' not found!"
    echo "   Please copy .env.production.example to $ENV_FILE and configure it."
    exit 1
fi

# Load environment file
set -a
source "$ENV_FILE"
set +a

# Function to check required variable
check_required() {
    local var_name=$1
    local var_value="${!var_name}"
    local min_length=${2:-1}
    
    if [ -z "$var_value" ]; then
        echo "❌ ERROR: $var_name is not set"
        ((ERRORS++))
        return 1
    elif [ ${#var_value} -lt $min_length ]; then
        echo "❌ ERROR: $var_name is too short (minimum $min_length characters)"
        ((ERRORS++))
        return 1
    else
        echo "✅ $var_name is set"
        return 0
    fi
}

# Function to check if value is still default/example
check_not_default() {
    local var_name=$1
    local var_value="${!var_name}"
    local default_pattern=$2
    
    if [[ "$var_value" =~ $default_pattern ]]; then
        echo "⚠️  WARNING: $var_name appears to be using default/example value"
        echo "   Current value: $var_value"
        ((ERRORS++))
        return 1
    fi
    return 0
}

# Function to validate password strength
check_password_strength() {
    local var_name=$1
    local var_value="${!var_name}"
    
    if [ ${#var_value} -lt 16 ]; then
        echo "⚠️  WARNING: $var_name should be at least 16 characters for production"
    fi
    
    if ! [[ "$var_value" =~ [A-Z] ]]; then
        echo "⚠️  WARNING: $var_name should contain uppercase letters"
    fi
    
    if ! [[ "$var_value" =~ [a-z] ]]; then
        echo "⚠️  WARNING: $var_name should contain lowercase letters"
    fi
    
    if ! [[ "$var_value" =~ [0-9] ]]; then
        echo "⚠️  WARNING: $var_name should contain numbers"
    fi
    
    if ! [[ "$var_value" =~ [^a-zA-Z0-9] ]]; then
        echo "⚠️  WARNING: $var_name should contain special characters"
    fi
}

echo ""
echo "📦 Database Configuration"
echo "------------------------"
check_required "POSTGRES_DB" 3
check_required "POSTGRES_USER" 3
check_required "POSTGRES_PASSWORD" 16
check_not_default "POSTGRES_PASSWORD" "CHANGE_ME|change_me|password|admin|test"
check_password_strength "POSTGRES_PASSWORD"
check_required "POSTGRES_DATA_PATH" 1

echo ""
echo "🔐 JWT Configuration"
echo "-------------------"
check_required "JWT_SECRET" 32
check_not_default "JWT_SECRET" "CHANGE_ME|change_me|secret|test"
check_password_strength "JWT_SECRET"
check_required "JWT_ISSUER" 3
check_required "JWT_AUDIENCE" 3

echo ""
echo "🔑 Google OAuth Configuration"
echo "-----------------------------"
check_required "GOOGLE_CLIENT_ID" 10
check_required "GOOGLE_CLIENT_SECRET" 10
check_not_default "GOOGLE_CLIENT_ID" "your-|example|test"
check_not_default "GOOGLE_CLIENT_SECRET" "your-|example|test"

echo ""
echo "🌐 CORS Configuration"
echo "--------------------"
check_required "CORS_ORIGINS" 5
if [[ "$CORS_ORIGINS" == *"localhost"* ]]; then
    echo "⚠️  WARNING: CORS_ORIGINS contains 'localhost' - this may not be suitable for production"
fi

echo ""
echo "📁 File Storage Configuration"
echo "----------------------------"
check_required "FILE_STORAGE_TYPE" 1
check_required "FILE_STORAGE_PATH" 1
check_required "UPLOADS_PATH" 1

# Check if upload directories exist
if [ ! -d "$POSTGRES_DATA_PATH" ]; then
    echo "⚠️  WARNING: POSTGRES_DATA_PATH directory does not exist: $POSTGRES_DATA_PATH"
    echo "   Creating directory..."
    mkdir -p "$POSTGRES_DATA_PATH"
fi

if [ ! -d "$UPLOADS_PATH" ]; then
    echo "⚠️  WARNING: UPLOADS_PATH directory does not exist: $UPLOADS_PATH"
    echo "   Creating directory..."
    mkdir -p "$UPLOADS_PATH"
fi

echo ""
echo "🔒 Security Settings"
echo "-------------------"
check_required "PASSWORD_MIN_LENGTH" 1
check_required "RATE_LIMIT_ENABLED" 1

# Validate password minimum length
if [ "$PASSWORD_MIN_LENGTH" -lt 8 ]; then
    echo "⚠️  WARNING: PASSWORD_MIN_LENGTH should be at least 8 for production"
fi

echo ""
echo "📊 Logging Configuration"
echo "-----------------------"
check_required "LOG_LEVEL" 1

if [[ "$LOG_LEVEL" == "Debug" || "$LOG_LEVEL" == "Trace" ]]; then
    echo "⚠️  WARNING: LOG_LEVEL is set to $LOG_LEVEL - this may generate excessive logs in production"
    echo "   Recommended: Warning or Error"
fi

echo ""
echo "================================================"

if [ $ERRORS -eq 0 ]; then
    echo "✅ All required environment variables are configured!"
    echo ""
    echo "🚀 Ready to deploy with:"
    echo "   docker-compose -f docker-compose.prod.yml --env-file $ENV_FILE up -d"
    exit 0
else
    echo "❌ Found $ERRORS error(s) or warning(s) in configuration"
    echo ""
    echo "Please fix the issues above before deploying to production."
    exit 1
fi
