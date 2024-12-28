using Godot;

namespace BehindTheMoon.scenes;

public partial class Level : Node
{
    private ENetMultiplayerPeer _peer = new();
    private Upnp _upnp = new();
    private Control _menu;
    [Export] private PackedScene playerScene;

    private const string Address = "127.0.0.1";
    private const int Port = 9999;

    public override void _Ready()
    {
        _menu = GetNode<Control>("GUICanvasLayer/Menu");
        GetNode<Button>("GUICanvasLayer/Menu/Host").Pressed += OnHost;
        GetNode<Button>("GUICanvasLayer/Menu/Join").Pressed += OnJoin;
    }

    private void OnHost()
    {
        _upnp.Discover();
        _upnp.AddPortMapping(Port, Port, "godot_udp");
        
        _menu.Visible = false;
        _peer.CreateServer(Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += PeerConnected;
        SelfLoad();
    }

    private void OnJoin()
    {
        _upnp.Discover();
        var ip = _upnp.QueryExternalAddress();
        GD.Print(ip);
        
        _menu.Visible = false;
        _peer.CreateClient(ip, Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.ConnectionFailed += () => GD.Print("Connection Failed");
        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.ConnectedToServer += () =>
        {
            var player = SelfLoad();
            player.GetCamera().Current = true;
        };
    }

    private void PeerConnected(long id)
    {
        LoadPlayer(id, false);
        var player = GetNode<Player>(id.ToString());
        player.GetCamera().Current = false;
    }

    private Player SelfLoad()
    {
        LoadPlayer(Multiplayer.GetUniqueId(), true);
        var player = GetNode<Player>(Multiplayer.GetUniqueId().ToString());
        player.GetBody().Visible = false;
        return player;
    }
    
    private void LoadPlayer(long id, bool isClient)
    {
        var player = playerScene.Instantiate<Player>();
        player.Name = id.ToString();
        player.IsClient = isClient;
        player.SetPosition(new Vector3(GD.Randf(), 1f, GD.Randf()));
        AddChild(player);
    }
}