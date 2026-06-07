#!/bin/bash

# Abbruch bei Fehlern
set -e

echo "🚀 Starte X-Act Backend Setup..."

# 1. Sicherstellen, dass wir im Root-Verzeichnis sind
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"

# 2. Optionale Bereinigung der alten Docker-Daten (behebt den Tabelle-Existiert-Bereits-Fehler)
if [ "$1" == "--clean" ]; then
    echo "🧹 Lösche alte Container und Volumes (-v)..."
    docker compose down -v
else
    echo "🛑 Stoppe laufende Container..."
    docker compose down
fi

# 3. EF Core Migrationen zur Sicherheit lokal validieren/bauen
echo "🔨 Überprüfe .NET Projekt-Struktur..."
if [ -d "backend" ]; then
    cd backend
    dotnet build
    cd ..
else
    echo "❌ Fehler: 'backend'-Ordner wurde nicht gefunden!"
    exit 1
fi

# 4. Docker Compose mit frischem Build starten
echo "🐳 Starte Docker-Netzwerk und baue Container neu..."
docker compose up --build -d

echo "📊 Status der Container:"
docker compose ps

echo "--------------------------------------------------------"
echo "✅ Setup erfolgreich gestartet!"
echo "💡 Nutze 'docker compose logs -f backend', um die Migrationen live zu sehen."
echo "--------------------------------------------------------"