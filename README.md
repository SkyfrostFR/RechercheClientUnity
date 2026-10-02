# GazeboRobotClient — client Unity du jumeau TIAGo Dual

Côté **client** d'une architecture client / serveur :

```
 ┌──────────── client (ce dépôt) ────────────┐        ┌──────────── serveur ─────────────┐
 │ Unity · scène TiagoClient                  │        │ Docker · ROS 1 Noetic            │
 │ jumeau TIAGo Dual (IK, VR, fantôme,        │  WS    │ rosbridge :9090                  │
 │ caméra, profondeur) · ROS#                 │ ─────► │ contrôleurs ros_control          │
 │ StreamingAssets/twin_server.json ──────────┼──┘     │ Gazebo Classic · TIAGo Dual      │
 └────────────────────────────────────────────┘        └──────────────────────────────────┘
```

Le client peut tourner sur n'importe quel PC du réseau (Windows ou Linux). Il se
connecte au serveur Gazebo par rosbridge (WebSocket, JSON, port 9090).

## Configurer le serveur

`Assets/StreamingAssets/twin_server.json` :

```json
{
  "host": "192.168.1.171",
  "port": 9090,
  "gazeboIsAuthority": true
}
```

Au lancement, `TwinServerConfig` (objet de la scène `Assets/Scenes/TiagoClient.unity`)
lit ce fichier et redirige le `RosConnector` du jumeau vers `ws://host:port`, avant
qu'il ne se connecte. Le journal Unity affiche l'adresse utilisée :
`[TwinServerConfig] rosbridge server: ws://192.168.1.171:9090`.

Dans une application compilée, le fichier se trouve dans
`<Application>_Data/StreamingAssets/twin_server.json` : changer de serveur ne demande
pas de recompiler. Si le fichier est absent ou invalide, le client utilise
`localhost:9090`.

## Gazebo fait foi

Le prefab superpose deux robots : `Digital_Twin`, piloté par l'IK (cibles VR) et dont la
pose est envoyée à Gazebo, et `Digital Shadow`, qui recopie `/joint_states`. Avec
`gazeboIsAuthority: true` (par défaut), `TiagoGazeboAuthority` échange leurs matériaux au
lancement : le robot **opaque** est celui de Gazebo, il ne bouge que quand Gazebo bouge ;
le robot **translucide** est la consigne envoyée. Le journal Unity l'indique :
`[TiagoGazeboAuthority] Gazebo is the reference: ...`. `false` rétablit l'affichage
d'origine (robot piloté opaque, fantôme translucide).

## Lancer

1. Le serveur Gazebo doit tourner et être joignable sur `host:port`.
2. Ouvrir le projet avec Unity 6000.6.3, charger `Assets/Scenes/TiagoClient.unity`, Play.

## Linux

`Ruckig.dll` et `InverseKinematics.dll` sont des DLL Windows. Le projet contient leurs
équivalents Linux (`libRuckig.so`, `libInverseKinematics.so`, sources dans
`NativeLinux/`) avec la même API : le même projet tourne sous Windows et sous Linux.
L'IK Linux est une réimplémentation (même modèle, résultats non identiques au bit
près). Recompilation :

```bash
cmake -S NativeLinux -B NativeLinux/build -DCMAKE_BUILD_TYPE=Release
cmake --build NativeLinux/build
```

## À savoir

- rosbridge n'a pas d'authentification : tout appareil qui joint le port 9090 du
  serveur peut commander le robot. À réserver à un réseau de confiance.
- `twin_server.json` est lu avec `File.ReadAllText` : ça fonctionne sur PC (éditeur,
  Windows, Linux), pas dans une application Android autonome (casque Quest sans PC).
- Le prefab du jumeau contient deux composants `TiagoDualArmRosSync` actifs.
