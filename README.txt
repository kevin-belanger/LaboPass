LaboPass
========

LaboPass est une application Windows locale conçue pour les environnements de laboratoire.

Elle permet de conserver des identifiants de test, des mots de passe et des codes MFA associés à des comptes temporaires. Les données sont stockées localement dans un fichier JSON non chiffré. Elle peut aussi importer un code QR TOTP, générer les codes MFA et réafficher le code QR au besoin.

Utilisation prévue
------------------

LaboPass est destiné à un usage pédagogique, par exemple dans un laboratoire où des élèves créent des comptes ou des tenants temporaires.

L’application ne doit pas être utilisée pour stocker des comptes personnels, des accès réels ou des accès de production.

Démarrage
---------

Pour utiliser LaboPass :

1. Ouvrez le fichier LaboPass.exe.
2. Ajoutez un identifiant avec le bouton prévu à cet effet.
3. Inscrivez le libellé, le nom d’utilisateur et le mot de passe.
4. Importez le code QR MFA lorsque le compte utilise une authentification multifacteur.
5. Utilisez le code MFA affiché dans l’application lorsque Microsoft ou un autre service le demande.

Codes MFA et QR TOTP
--------------------

Un TOTP (Time-based One-Time Password) est un code de sécurité temporaire utilisé lors de l’authentification multifacteur (MFA). Il s’agit généralement d’un nombre à 6 chiffres qui change automatiquement toutes les 30 secondes et qui doit être saisi en plus du mot de passe lors de la connexion.

LaboPass peut importer un code QR contenant une URI TOTP.

Une fois le QR importé, l’application peut :

afficher le code MFA actuel;
afficher le temps restant avant le prochain code;
copier le code MFA;
réafficher le code QR associé au compte;
supprimer ou remplacer le QR associé à un identifiant.

Fichier de données
------------------

LaboPass enregistre les identifiants dans un fichier JSON local.

Par défaut, ce fichier se nomme vault.json et se trouve dans le même dossier que LaboPass.exe.

Important : les données enregistrées dans ce fichier ne sont pas chiffrées. Toute personne ayant accès au fichier vault.json peut consulter son contenu, y compris les noms d’utilisateur, les mots de passe, les URI TOTP et les notes enregistrées.

Il est aussi possible d’ouvrir un autre fichier JSON à partir de l’application si vous souhaitez utiliser un autre coffre local.

Avertissement
-------------

LaboPass est un outil de laboratoire conçu pour des comptes temporaires et des environnements de test.

Les données étant stockées dans un fichier JSON non chiffré, l’application ne doit pas être utilisée pour conserver des mots de passe personnels, des comptes réels ou des accès de production.

LaboPass ne remplace pas un gestionnaire de mots de passe professionnel comme KeePass, Bitwarden, 1Password ou une solution équivalente.

Projet GitHub
-------------

Le projet est disponible ici :

https://github.com/kevin-belanger/LaboPass

Licence
-------

LaboPass est un projet open source distribué sous la licence MIT.

La licence MIT est l’une des licences open source les plus permissives. Elle autorise l’utilisation, la modification, la distribution, la publication et l’intégration du code dans des projets privés ou commerciaux, à condition de conserver l’avis de droit d’auteur et le texte de la licence.

Conformément aux termes de cette licence, le logiciel est fourni « tel quel », sans aucune garantie, expresse ou implicite. Son utilisation se fait sous la seule responsabilité de l’utilisateur, qui assume l’ensemble des risques liés à son installation, son utilisation ou ses éventuelles conséquences.

Le texte complet de la licence est disponible dans le fichier LICENSE à la racine du dépôt GitHub.


Crédits
-------

Certaines icônes utilisées dans l’application proviennent de Flaticon.

Password icons created by Smashicons - Flaticon:
https://www.flaticon.com/free-icons/password