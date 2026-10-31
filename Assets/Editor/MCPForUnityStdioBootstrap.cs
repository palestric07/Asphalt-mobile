using UnityEngine;
using UnityEditor;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;

/// <summary>
/// One-shot helper so OpenCode's MCP client can reach this project over stdio.
/// Forces the MCP for Unity transport to stdio (HTTP is the package default) and
/// starts the stdio bridge on editor load. Safe to delete once the bridge is wired up;
/// the transport preference persists in EditorPrefs afterwards.
/// </summary>
public static class MCPForUnityStdioBootstrap
{
    private static int _tries;

    [InitializeOnLoadMethod]
    private static void Bootstrap()
    {
        // Persist stdio transport + refresh the package's config cache.
        EditorConfigurationCache.Instance.SetUseHttpTransport(false);

        if (StdioBridgeHost.IsRunning)
            return;

        // Retry on the editor loop until compilation settles and the bridge is listening.
        EditorApplication.update += TryStartBridge;
    }

    private static void TryStartBridge()
    {
        if (StdioBridgeHost.IsRunning)
        {
            EditorApplication.update -= TryStartBridge;
            Debug.Log("[MCP Stdio Bootstrap] Bridge already running.");
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return; // wait for the editor to settle

        if (++_tries > 300)
        {
            EditorApplication.update -= TryStartBridge;
            Debug.LogWarning("[MCP Stdio Bootstrap] Gave up waiting to start the bridge.");
            return;
        }

        try
        {
            StdioBridgeHost.Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[MCP Stdio Bootstrap] Start failed: " + ex.Message);
        }

        if (StdioBridgeHost.IsRunning)
        {
            EditorApplication.update -= TryStartBridge;
            Debug.Log("[MCP Stdio Bootstrap] Bridge started on port " + StdioBridgeHost.GetCurrentPort());
        }
    }
}
