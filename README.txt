LaboPass
========

LaboPass est une petite application Windows locale destinee aux environnements pedagogiques de laboratoire.
Elle permet de conserver des identifiants de test, des mots de passe et des URI TOTP completes utilisees pour generer des codes MFA.

AVERTISSEMENT IMPORTANT
-----------------------
LaboPass est un outil pedagogique local destine aux environnements de laboratoire.
Il ne doit pas etre utilise pour stocker des mots de passe reels, des comptes personnels ou des acces de production.
Le fichier vault.json est stocke en clair dans cette premiere version.

Utilisation
-----------
- Lancer LaboPass.exe.
- Cliquer sur "Ajouter un identifiant" pour enregistrer un libelle, un nom d'utilisateur, un mot de passe, une URI TOTP complete et des notes.
- Dans le formulaire d'ajout ou modification, utiliser "Coller le QR depuis le presse-papiers" apres avoir copie une image QR contenant une URI otpauth://.
- Les codes MFA/TOTP sont rafraichis automatiquement.
- Utiliser "Afficher le QR" pour regenerer un QR code a partir de l'URI TOTP complete enregistree.

Stockage
--------
L'application lit et ecrit vault.json dans le meme dossier que LaboPass.exe.
Si vault.json est absent, il est cree automatiquement.
Si vault.json est vide ou invalide, LaboPass affiche un message clair et repart avec une liste vide.

Publication portable
--------------------
Depuis la racine du depot, executer:

    .\source\build.ps1

Le script publie l'application en Release, win-x64, self-contained, single-file, puis copie LaboPass.exe a la racine.
Les fichiers temporaires de build restent dans source\bin.

Credits
-------
Password icons created by Smashicons - Flaticon - https://www.flaticon.com/free-icons/password
