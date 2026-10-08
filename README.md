# RechercheClientUnity

Client Unity (VR, Meta Quest 3) du jumeau numérique du robot TIAGo Dual. Le serveur
Gazebo est le dépôt
[RechercheGazeboServeur](https://github.com/SkyfrostFR/RechercheGazeboServeur).

## Architecture

```
 ┌──────── client (ce dépôt) ────────┐   WebSocket   ┌──────── serveur ─────────┐
 │ Unity 6 · scène TiagoClient       │  JSON :9090   │ Docker · ROS 1 Noetic    │
 │ jumeau du robot, IK, VR, ROS#     │ ────────────► │ rosbridge                │
 │                                   │ ◄──────────── │ Gazebo · TIAGo Dual      │
 └───────────────────────────────────┘               └──────────────────────────┘
```

- **Client → serveur** : la pose des bras (pilotés en VR par l'IK), les pinces et le
  déplacement de la base.
- **Serveur → client** : l'état réel des articulations, la caméra, la profondeur et
  la position des objets. Gazebo fait foi : le robot opaque dans Unity est celui de
  Gazebo, le robot translucide est la consigne envoyée.

## Installation

1. Installer **Unity 6000.6.3** avec Unity Hub (Windows ou Linux).
2. Cloner le dépôt :

   ```bash
   git clone https://github.com/SkyfrostFR/RechercheClientUnity.git
   ```

3. Indiquer l'adresse du serveur dans `Assets/StreamingAssets/twin_server.json`. Les
   serveurs sont essayés dans l'ordre, le premier qui répond est utilisé :

   ```json
   {
     "servers": [
       { "name": "local", "host": "localhost", "port": 9090 },
       { "name": "lab-server", "host": "192.168.1.171", "port": 9090 }
     ]
   }
   ```

## Lancer

1. Lancer le serveur (`pixi run sim` dans RechercheGazeboServeur).
2. Ouvrir le dossier du projet depuis Unity Hub (*Add project from disk*).
3. Ouvrir la scène `Assets/Scenes/TiagoClient.unity` et appuyer sur Play.
4. Pour la VR : relier le Quest 3 au PC par Quest Link ou Air Link, avec Meta comme
   runtime OpenXR.

## Commandes VR

| Action | Manette Quest 3 |
|---|---|
| Déplacer un bras | approcher la main du bout du bras, maintenir **grip** |
| Fermer la pince | maintenir la **gâchette** |
| Avancer / reculer | joystick gauche |
| Tourner | joystick droit |

Attention : rosbridge n'a pas d'authentification. À utiliser sur un réseau de confiance.
