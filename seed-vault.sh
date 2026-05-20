#!/usr/bin/env bash
set -euo pipefail

VAULT_SERVICE="${VAULT_SERVICE:-vault}"
VAULT_ADDR="${VAULT_ADDR:-https://127.0.0.1:8201}"
VAULT_TOKEN="${VAULT_TOKEN:-00000000-0000-0000-0000-000000000000}"
VAULT_SKIP_VERIFY="${VAULT_SKIP_VERIFY:-true}"

AUTH_FILE="${AUTH_FILE:-vault-auth.json}"
MONGO_FILE="${MONGO_FILE:-vault-mongo.json}"

if [[ ! -f "$AUTH_FILE" ]]; then
  echo "Missing $AUTH_FILE"
  exit 1
fi

if [[ ! -f "$MONGO_FILE" ]]; then
  echo "Missing $MONGO_FILE"
  exit 1
fi

if docker compose ps -q "$VAULT_SERVICE" >/dev/null 2>&1 && [[ -n "$(docker compose ps -q "$VAULT_SERVICE")" ]]; then
  VAULT_CONTAINER="$(docker compose ps -q "$VAULT_SERVICE")"
else
  VAULT_CONTAINER="${VAULT_CONTAINER:-vault}"
fi

echo "Using Vault container: $VAULT_CONTAINER"
echo "Using Vault address inside container: $VAULT_ADDR"
echo

echo "Copying secret JSON files into Vault container..."

docker cp "$AUTH_FILE" "$VAULT_CONTAINER:/tmp/vault-auth.json"
docker cp "$MONGO_FILE" "$VAULT_CONTAINER:/tmp/vault-mongo.json"

echo "Seeding Vault secrets..."

docker exec \
  -e VAULT_ADDR="$VAULT_ADDR" \
  -e VAULT_TOKEN="$VAULT_TOKEN" \
  -e VAULT_SKIP_VERIFY="$VAULT_SKIP_VERIFY" \
  "$VAULT_CONTAINER" \
  sh -c '
    set -e

    echo "Checking Vault status..."
    vault status >/dev/null

    echo "Ensuring KV v2 engine exists at secret/..."

    if vault secrets list | grep -q "^secret/"; then
      echo "secret/ engine already exists"
    else
      vault secrets enable -path=secret kv-v2
    fi

    echo "Writing secret/auth..."
    vault kv put secret/auth @/tmp/vault-auth.json >/dev/null

    echo "Writing secret/mongo..."
    vault kv put secret/mongo @/tmp/vault-mongo.json >/dev/null

    rm -f /tmp/vault-auth.json /tmp/vault-mongo.json

    echo "Done."
  '

echo
echo "Vault secrets created:"
echo "  secret/auth"
echo "  secret/mongo"
