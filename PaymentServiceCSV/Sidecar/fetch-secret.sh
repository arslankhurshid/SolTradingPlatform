#!/bin/sh
set -e

# ================================
# Fake-Azure-Modus
# ================================
# Da der Zugriff auf Azure Key Vault �ber 'az login' oder Service Principals
# mangels Berechtigungen nicht m�glich war, simuliert dieses Skript
# den Secret-Zugriff lokal. Es schreibt ein statisches Secret in ein Shared Volume.
#
# So bleibt das Sidecar-Pattern technisch korrekt umgesetzt,
# ohne dass ein echter Cloud-Zugriff n�tig ist.

echo "Starte Fake-Modus - kein echter Azure-Zugriff."

printf "%s" "$FAKE_SECRET" > /secrets/payment-api-key.txt

echo "Secret gespeichert unter /secrets/payment-api-key.txt"



# ================================
# Echte Azure-Umsetzung (optional)
# ================================
# Bei verf�gbaren Azure-Rechten kann folgender Code verwendet werden:

# echo "Hole Secret aus Azure Key Vault: $KEYVAULT_NAME"

# SECRET=$(az keyvault secret show \
#   --vault-name "$KEYVAULT_NAME" \
#   --name "PaymentApiKey" \
#   --query value -o tsv)

# echo "$SECRET" > /secrets/payment-api-key.txt
# echo "Secret geschrieben nach /secrets/payment-api-key.txt"
