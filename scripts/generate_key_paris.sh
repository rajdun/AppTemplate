#!/bin/bash

# 1. Sprawdzenie, czy użytkownik podał argument (ścieżkę)
if [ -z "$1" ]; then
  echo "❌ Błąd: Nie podano ścieżki docelowej."
  echo "💡 Użycie: $0 <sciezka/nazwa_bazowa>"
  echo "Przykład: $0 ./secrets/prod_jwt"
  exit 1
fi

BASE_PATH=$1
PRIVATE_KEY="${BASE_PATH}_private.pem"
PUBLIC_KEY="${BASE_PATH}_public.pem"

echo "⏳ Generowanie kluczy RSA (2048-bit)..."

# 2. Generowanie klucza prywatnego (bez hasła dzięki -N "")
ssh-keygen -t rsa -b 2048 -m PEM -f "$PRIVATE_KEY" -N "" -q
if [ $? -ne 0 ]; then
    echo "❌ Błąd podczas generowania klucza prywatnego."
    exit 1
fi

# 3. Wyciąganie klucza publicznego
openssl rsa -in "$PRIVATE_KEY" -pubout -outform PEM -out "$PUBLIC_KEY" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "❌ Błąd podczas generowania klucza publicznego."
    exit 1
fi

echo "✅ Gotowe! Twoje klucze znajdują się tutaj:"
echo " 🔒 Prywatny: $PRIVATE_KEY"
echo " 🔓 Publiczny: $PUBLIC_KEY"
