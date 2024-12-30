using Godot;

namespace BehindTheMoon.scenes;

public partial class Level : Node
{
    private ENetMultiplayerPeer _peer = new();
    private Control _menu;
    [Export] private PackedScene playerScene;

    private const string Address = "87.92.56.186";
    private const int Port = 9999;

    public override void _Ready()
    {
        _menu = GetNode<Control>("GUICanvasLayer/Menu");
        GetNode<Button>("GUICanvasLayer/Menu/Host").Pressed += OnHost;
        GetNode<Button>("GUICanvasLayer/Menu/Join").Pressed += OnJoin;
    }

    private void OnHost()
    {
        _menu.Visible = false;
        _peer.CreateServer(Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += PeerConnected;
        SelfLoad();
    }

    private void OnJoin()
    {
        _menu.Visible = false;
        _peer.CreateClient(Address, Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += PeerConnected;
        ToSignal(GetTree().CreateTimer(0.1f), "timeout").OnCompleted(() =>
        {
            var player = SelfLoad();
            player.GetCamera().Current = true;
        });
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
        player.SetInvisible();
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