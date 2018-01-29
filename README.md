# RobotMotionPlanning

## Auteurs

Mickaël Laurent et Mathis Petrovich

## Le projet

Ce projet est le calcul et la simulation du déplacement d'une voiture. On utilise un algorithme Monte-Carlo suivi d'un calcul des courbes Reed&Sheep. 

## Utilisation

Au démarrage du programme, il y a deux onglets :
- Paramétrage de la résolution d'écran
- Paramétrage des touches du clavier pour l'utilisation

Dans le menu de chargement de cartes, on peut changer le rayon de braquage de la voiture. La sélection de la carte se fait avec la souris.

Lorsqu'une carte est chargé, on sélectionne à la souris l'endroit où on veut placer la voiture au départ puis la position d'arrivée.
On peut faire un clic droit pour revenir au choix précédent.

On choisi alors si on veut regarder :

- La trajectoire de Monte-Carlo (ce n'est pas une trajectoire de voiture, mais c'est sans collision)
- La trajectoire Reed&Shepp (trajectoire de voiture mais avec collisions possibles)
- La trajectoire qui combine les deux (trajectoire de voiture sans collision)

## Compilation

Il suffit de télécharger unity, puis d'aller dans le menu File -> Build settings.

## Installation sur MacOS

Après avoir extrait le projet, il suffit de copier ReedAndShepp64.dylib et ReedAndShepp.dylib dans le dossier /usr/local/lib de votre ordinateur.

Pour cela, ouvrez un terminal (https://fr.wikihow.com/ouvrir-le-Terminal-sur-un-Mac) et entrez (en remplaçant ~/Downloads/RMP_Mac/ par le chemin vers votre projet s'il est différent) :

```bash
cd ~/Downloads/RMP_Mac/
cp ReedAndShepp64.dylib ReedAndShepp.dylib /usr/local/lib
```

Il suffit ensuite de lancer rmp.app.

NOTE 1 : Si les paramètres de sécurité vous empêchent de lancer l'application, vous pouvez suivre ces instructions : https://support.apple.com/kb/PH25088?viewlocale=fr_FR&locale=en_US.

NOTE 2 : Si le calcul des courbes de Reed&Shepp ne fonctionne pas (aucun déplacement de la voiture), veuillez vérifier que vous avez bien copié les deux fichiers dylib dans /usr/local/lib comme indiqué précedemment.

## Installation sur Linux

Il suffit de lancer rmp._x86_64.

## Installation sur Windows

Il suffit de lancer rmp.exe.
