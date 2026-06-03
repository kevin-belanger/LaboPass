LaboPass
========

LaboPass est une petite application Windows locale destinée aux environnements pédagogiques de laboratoire.
Elle conserve des identifiants de tenants Microsoft 365 temporaires créés par des élèves et les URI TOTP/MFA associées.

AVERTISSEMENT IMPORTANT
-----------------------
LaboPass est un outil pédagogique local destiné aux environnements de laboratoire.
Il ne doit pas être utilisé pour stocker des mots de passe réels, des comptes personnels ou des accès de production.
Le fichier vault.json est stocké en clair dans cette première version.

Utilisation
-----------
- Lancer LaboPass.exe.
- Cliquer sur "Ajouter un identifiant" pour enregistrer un libellé, un nom d'utilisateur, un mot de passe, une URI TOTP complète et des notes.
- Dans le formulaire d'ajout ou modification, utiliser "Coller le QR depuis le presse-papiers" après avoir copié une image QR depuis Microsoft.
- Les codes MFA/TOTP sont rafraîchis automatiquement.
- Utiliser "Afficher le QR" pour régénérer un QR code à partir de l'URI TOTP complète enregistrée.

Stockage
--------
L'application lit et écrit vault.json dans le même dossier que LaboPass.exe.
Si vault.json est absent, il est créé automatiquement.
Si vault.json est vide ou invalide, LaboPass affiche un message clair et repart avec une liste vide.

Publication portable
--------------------
Depuis la racine du dépôt, exécuter:

    .\build.ps1

Le script publie l'application en Release, win-x64, self-contained, single-file, puis copie LaboPass.exe à la racine.
Le code source reste dans le dossier source.

Password icons created by Smashicons - Flaticon - https://www.flaticon.com/free-icons/password