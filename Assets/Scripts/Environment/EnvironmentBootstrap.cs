using UnityEngine;

/// <summary>
/// Runtime entry point for the procedural town. Lives in the saved scene and
/// builds the environment in Awake.
///
/// This must be runtime generation (not editor generation) because meshes and
/// materials created during the editor setup session are not serialized into
/// the scene - the same class of problem that previously erased the menu
/// button listeners.
/// </summary>
[DefaultExecutionOrder(-80)]
public class EnvironmentBootstrap : MonoBehaviour
{
    public int seed = 20261024;
    public bool generateOnAwake = true;

    private bool generated;

    private void Awake()
    {
        if (generateOnAwake) Generate();
    }

    public void Generate()
    {
        if (generated) return;
        if (GameObject.Find("ProceduralTown") != null)
        {
            generated = true;
            return;
        }

        generated = true;
        Application.targetFrameRate = 60;

        ProceduralTownGenerator.Generate(seed);

        if (GetComponent<WitchVisualIntegrator>() == null)
            gameObject.AddComponent<WitchVisualIntegrator>();

        if (VisualProbe.Requested && GetComponent<VisualProbe>() == null)
            gameObject.AddComponent<VisualProbe>();

        if (CinematicProbe.Requested && GetComponent<CinematicProbe>() == null)
            gameObject.AddComponent<CinematicProbe>();

        if (PerfLogger.Requested && GetComponent<PerfLogger>() == null)
            gameObject.AddComponent<PerfLogger>();

        if (NavProbe.Requested && GetComponent<NavProbe>() == null)
            gameObject.AddComponent<NavProbe>();
    }
}
