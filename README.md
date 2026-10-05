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
  "servers": [
    { "name": "local", "host": "localhost", "port": 9090 },
    { "name": "lab-server", "host": "192.168.1.171", "port": 9090 }
  ],
  "probeTimeoutMs": 400,
  "gazeboIsAuthority": true
}
```

Au lancement, `TwinServerConfig` essaie les serveurs **dans l'ordre** (connexion TCP,
`probeTimeoutMs` chacun) et redirige le `RosConnector` du jumeau vers le premier qui
répond, avant qu'il ne se connecte. Sur le PC du serveur c'est `localhost` ; depuis un
autre PC (Windows 11 par exemple) `localhost` ne répond pas et le client passe au
serveur du réseau. Si aucun ne répond, le premier est utilisé et ROS# réessaie. Le
journal Unity indique le choix :
`[TwinServerConfig] rosbridge server: lab-server (ws://192.168.1.171:9090)`.

Dans une application compilée, le fichier se trouve dans
`<Application>_Data/StreamingAssets/twin_server.json` : changer de serveur ne demande
pas de recompiler. L'ancien format (`"host"`, `"port"` seuls) est toujours accepté. Si
le fichier est absent ou invalide, le client utilise `localhost:9090`.

## Gazebo fait foi

Le prefab superpose deux robots : `Digital_Twin`, piloté par l'IK (cibles VR) et dont la
pose est envoyée à Gazebo, et `Digital Shadow`, qui recopie `/joint_states`. Avec
`gazeboIsAuthority: true` (par défaut), `TiagoGazeboAuthority` échange leurs matériaux au
lancement : le robot **opaque** est celui de Gazebo, il ne bouge que quand Gazebo bouge ;
le robot **translucide** est la consigne envoyée. Le journal Unity l'indique :
`[TiagoGazeboAuthority] Gazebo is the reference: ...`. `false` rétablit l'affichage
d'origine (robot piloté opaque, fantôme translucide).

## Lancer

1. Serveur (PC Linux) : `pixi run ros1-sim` dans StageIR. La fenêtre Gazebo s'ouvre
   (`GUI=false` pour s'en passer) ; rosbridge écoute sur le port 9090 de toutes les
   interfaces.
2. Client : ouvrir ce projet avec **Unity 6000.6.3** (Windows ou Linux ;
   `pixi run unity-client` sur le serveur), charger `Assets/Scenes/TiagoClient.unity`,
   Play. Les bras vont en position d'accueil, puis suivent l'IK (cibles VR) en mode
   Position.
3. Quest 3 : casque relié au PC Windows (Quest Link / Air Link, runtime OpenXR Meta).

## Synchronisation des bras

- Unity → Gazebo : `TiagoDualArmRosSync` envoie la pose du robot piloté par l'IK aux
  contrôleurs `arm_left_controller` / `arm_right_controller` (20 Hz).
- Gazebo → Unity : `TiagoShadowFromRos` recopie `/joint_states` sur le robot opaque.
- Le prefab contient deux `TiagoDualArmRosSync` identiques ; `TwinSingleArmSync` désactive
  le doublon au lancement (journal : `[TwinSingleArmSync] disabled duplicate ...`), sinon
  chaque commande partait deux fois et passer `mode` à Off dans l'Inspector ne suffisait
  pas à libérer Gazebo. Le prefab n'est pas modifié.

### Test automatique (sans casque)

Lancer l'application avec `-twinProbe` installe `TwinSyncProbe` : une fois les bras en
accueil, il déplace les cibles IK des deux bras (boucle de ±8 cm, 24 s), mesure l'écart
articulaire Unity ↔ `/joint_states`, puis cesse de commander pendant 20 s pour qu'un
script bouge Gazebo depuis ROS et vérifie que le robot Unity suit. Résultat :
`[TwinSyncProbe] RESULT PASS|FAIL ...` (tolérance 3°, `-twinProbeQuit` quitte ensuite).
Côté serveur, `pixi run twin-probe` fait tout (build Linux par
`TwinClientBuild.BuildLinux`, sonde, mouvement Gazebo). Mesures du 2026-10-05 : écart
max 1,5° pendant le mouvement, 0,2° à l'arrêt, 0,00° Gazebo → Unity, ~20° de course.

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
- À la fermeture, ROS# a déjà coupé la socket quand les scripts se désabonnent : les
  `KeyNotFoundException` dans `RosSocket.Unsubscribe` à ce moment-là sont sans effet.
- Le casque doit être en mode PC (Link) : une application Quest autonome ne lit pas
  `twin_server.json` (voir plus haut).
