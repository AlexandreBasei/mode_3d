# Modélisation 3D - TP3
## Installation
- Créer un nouveau projet Unity (version >= à 6000.2.9f1)
- Importer les fichiers depuis le repo à la racine du projett
- Ouvrir la scène "Mode3d_tp3" dans le dossier Assets > Scenes

### Initialisation manuelle
- Créer un GameObject vide
- Ajouter le script "Octree" au GameObject
- Créer un cube
- Ajouter le cube au GameObject contenant le script "Octree" dans l'inspecteur (cube prefab)
- Paramétrer l'octree :
    - le paramètre "octree depth" permet d'augmenter la résolution, affichant plus de cubes (attention crashs d'unity avec des valeurs trop élevées, rester autour de 5/6)
    - il est possible d'ajouter des sphères et des cubes manuellement depuis l'inspecteur et de définir leur position et leur taille
    - plusieurs blend modes sont disponibles : union et intersection
- Lancer le jeu

### Ex 6 : tooling
Techniquement l'exercice 6 est fait mais il ne permet que d'ajouter et de supprimer des cubes dans une zone restreinte. Pour l'initialiser manuellement :
- Créer un GameObject vide
- Ajouter le script "VoxelVolume" au GameObject
    - Ajouter le cube au VoxelVolume dans l'inspecteur (voxel prefab)
    - Configurer VoxelVolume (la taille ds voxels par exemple)
- Créer un autre GameObject vide
- Lui ajouter le script "VoxelBrush"
    - La taille du pinceau est ajustable si besoin
- Lancer le jeu : clic gauche pour peindre et clic droit pour effacer
