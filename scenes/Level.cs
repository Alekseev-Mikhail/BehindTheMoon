using System.Threading.Tasks;
using Godot;

namespace BehindTheMoon.scenes;

public partial class Level : Node
{
    private ENetMultiplayerPeer _peer = new();
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
        _menu.Visible = false;
        _peer.CreateServer(Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        LoadPlayer(Multiplayer.GetUniqueId(), true);
    }

    private void OnJoin()
    {
        _menu.Visible = false;
        _peer.CreateClient(Address, Port);
        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        ToSignal(GetTree().CreateTimer(0.1f), "timeout").OnCompleted(() =>
        {
            LoadPlayer(Multiplayer.GetUniqueId(), true);
            GetNode<Player>(Multiplayer.GetUniqueId().ToString()).GetCamera().Current = true;
        });
    }

    private void OnPeerConnected(long id)
    {
        LoadPlayer(id, false);
        GetNode<Player>(id.ToString()).GetCamera().Current = false;
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