#!/bin/bash
# Script d'initialisation du VPS STS
# À lancer une seule fois en root après réception du serveur :
#   bash setup-vps.sh <deploy_public_key>
#
# Exemple :
#   bash setup-vps.sh "ssh-ed25519 AAAA... sts-deploy"

set -e

DEPLOY_USER="sts"
DEPLOY_KEY="$1"

if [ -z "$DEPLOY_KEY" ]; then
    echo "Usage: bash setup-vps.sh \"<clé publique ssh>\""
    exit 1
fi

echo "=== Mise à jour du système ==="
apt-get update && apt-get upgrade -y

echo "=== Installation de Docker ==="
curl -fsSL https://get.docker.com | sh

echo "=== Création de l'utilisateur de déploiement : $DEPLOY_USER ==="
useradd -m -s /bin/bash "$DEPLOY_USER"
usermod -aG docker "$DEPLOY_USER"

echo "=== Configuration SSH pour $DEPLOY_USER ==="
mkdir -p "/home/$DEPLOY_USER/.ssh"
echo "$DEPLOY_KEY" > "/home/$DEPLOY_USER/.ssh/authorized_keys"
chmod 700 "/home/$DEPLOY_USER/.ssh"
chmod 600 "/home/$DEPLOY_USER/.ssh/authorized_keys"
chown -R "$DEPLOY_USER:$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh"

echo "=== Création du dossier de déploiement ==="
mkdir -p "/home/$DEPLOY_USER/sts"
chown "$DEPLOY_USER:$DEPLOY_USER" "/home/$DEPLOY_USER/sts"

echo "=== Configuration du pare-feu ==="
apt-get install -y ufw
ufw allow OpenSSH
ufw allow 8080/tcp
ufw --force enable

echo ""
echo "=== Setup terminé ! ==="
echo "Prochaines étapes :"
echo "  1. Copier le data.json sur le serveur :"
echo "     scp data.json $DEPLOY_USER@<IP>:~/sts/data.json"
echo "  2. Ajouter les secrets GitHub :"
echo "     VPS_HOST  = <IP du VPS>"
echo "     VPS_USER  = $DEPLOY_USER"
echo "  3. Pusher sur master pour déclencher le premier déploiement"
