#!/bin/bash
cd "$(dirname "$0")"

echo "-----------------------------------------------------"
echo "[1/6] Initializing Git repository in existing folder..."
git init

echo "[2/6] Disabling line ending conversion to prevent rewrites..."
git config core.autocrlf false
git config core.eol lf

echo "[3/6] Adding remote origin..."
git remote add origin https://github.com/Unity-Environmental-University/Horticulture-Scripts.git

echo "[4/6] Fetching latest from origin/main..."
git fetch origin main

echo "[5/6] Creating local branch 'main' pointing to origin/main (NO CHECKOUT)..."
git symbolic-ref HEAD refs/heads/main
git reset origin/main

echo "[6/6] Done! No files were overwritten. Git is now tracking 'origin/main'."
read -n 1 -s -r -p "Press any key to continue..."
echo
