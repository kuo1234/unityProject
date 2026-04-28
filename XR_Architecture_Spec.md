# VR Garbage Sorting Game - Unity Architecture Spec
Please generate the following 5 C# scripts. Ensure they use the Meta XR Interaction SDK where applicable.

1. **TrashItem.cs** (in Scripts/Core)
   - MonoBehaviour with a public enum `TrashCategory` (General, Recyclable_Plastic, Recyclable_Paper, FoodWaste_Raw, FoodWaste_Cooked).
   - Public fields: `TrashCategory itemType`, `bool isDirty = false`, `bool isCompoundItem = false`. No update logic.

2. **TrashSpawner.cs** (in Scripts/Environment)
   - Public array of GameObjects `trashPrefabs`, public Transform `spawnPoint`, float `spawnInterval`. 
   - Coroutine to instantiate a random prefab at `spawnPoint` every `spawnInterval` seconds.
   - Upon instantiation, apply a slow continuous Rigidbody AddForce along the local Z-axis.

3. **BinValidator.cs** (in Scripts/Logic)
   - Requires IsTrigger Collider. Public `TrashCategory targetCategory`.
   - `OnTriggerEnter`: If the colliding object has a `TrashItem`, compare `itemType`.
   - If match & `isDirty == false`: Destroy object, Debug.Log success.
   - If fail: Rigidbody AddForce upward/outward, trigger haptic error via `OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All)`.

4. **WashingStation.cs** (in Scripts/Interactions)
   - `OnTriggerStay`: If a `TrashItem` is dirty, start a 2-second timer. 
   - If held for 2 seconds, set `isDirty = false`, swap MeshRenderer material to a public `cleanMaterial`, and Debug.Log success. Reset timer if it exits early.

5. **DeconstructableItem.cs** (in Scripts/Interactions)
   - For a parent object with a child `lidObject`. 
   - In Update, check Vector3.Distance between parent and child. 
   - If distance > 0.2f, unparent `lidObject`, set its Rigidbody `isKinematic = false`, disable this script, and Debug.Log separation.