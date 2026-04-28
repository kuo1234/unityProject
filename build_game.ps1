# Meta Quest VR Garbage Sorting Game - Automated Agent Execution
# Run this in C:\Users\andreas\unityProject

Write-Host "Phase 1: Setting up Data Models and Enums" -ForegroundColor Cyan
codex exec --full-auto "Create a C# script named 'TrashItem.cs' in Scripts/Core. It should be a MonoBehaviour. Include a public enum called 'TrashCategory' with values: General, Recyclable_Plastic, Recyclable_Paper, FoodWaste_Raw, FoodWaste_Cooked. Add public fields to the class: 'TrashCategory itemType', 'bool isDirty = false', and 'bool isCompoundItem = false'. Do not include any update logic."

Write-Host "Phase 2: The Dynamic Spawner (Conveyor Belt)" -ForegroundColor Cyan
codex exec --full-auto "Create a C# script named 'TrashSpawner.cs' in Scripts/Environment. It should have a public array of GameObjects called 'trashPrefabs', a public Transform 'spawnPoint', and a float 'spawnInterval'. Use a Coroutine to instantiate a random prefab from the array at the spawnPoint every 'spawnInterval' seconds. Upon instantiation, get the object's Rigidbody component and apply a continuous slow physical force along the local Z-axis to simulate a moving conveyor belt."

Write-Host "Phase 3: Bin Validation Logic" -ForegroundColor Cyan
codex exec --full-auto "Create a C# script named 'BinValidator.cs' in Scripts/Logic. It requires a Collider component set to IsTrigger. Include a public 'TrashCategory targetCategory'. Use OnTriggerEnter to detect if the entering Collider has a 'TrashItem' component. If it does, compare its 'itemType' to 'targetCategory'. If it matches AND 'isDirty' is false, Destroy the object and Debug.Log '[Success] Sorted correctly'. If it fails (wrong category or isDirty is true), get the object's Rigidbody, apply a sudden upward and outward AddForce to bounce it out, and use OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All) to trigger a haptic error rumble."

Write-Host "Phase 4: The Washing Station" -ForegroundColor Cyan
codex exec --full-auto "Create a C# script named 'WashingStation.cs' in Scripts/Interactions. It needs an OnTriggerStay method. If a GameObject with a 'TrashItem' component enters and its 'isDirty' field is true, start a 2-second timer. If the item stays in the trigger for the full 2 seconds, set 'isDirty' to false, swap the object's MeshRenderer material to a public 'cleanMaterial' assigned in the inspector, and Debug.Log '[Wash] Item is now clean'. Reset the timer if the object exits the trigger early."

Write-Host "Phase 5: XR Deconstruction Logic" -ForegroundColor Cyan
codex exec --full-auto "Create a C# script named 'DeconstructableItem.cs' in Scripts/Interactions. Assume the use of the Meta XR Interaction SDK. This script goes on a parent object (e.g., a Boba Cup) that has a child object (e.g., the Plastic Lid). Include a reference to the child GameObject 'lidObject'. In the Update loop, check the Vector3.Distance between the parent and the 'lidObject'. If the distance exceeds 0.2f units (simulating the user pulling them apart with two hands), unparent the 'lidObject', enable its Rigidbody isKinematic = false, and disable this script to prevent it from firing again. Debug.Log '[Deconstruct] Item separated'."

Write-Host "All base systems generated. Proceed to Unity Editor for Simulator testing." -ForegroundColor Green