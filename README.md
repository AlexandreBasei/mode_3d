# Modélisation 3D - TP2
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
