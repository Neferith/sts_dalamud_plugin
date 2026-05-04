using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Sts.Domain;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Sts.Domain.Character;

namespace STSPlugin.Windows;

/// <summary>
/// Barre de raccourcis flottante. Affiche les actions sélectionnées du personnage actif.
/// </summary>
public class QuickbarWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    private bool _editMode = false;
    private string _newActionName = string.Empty;
    private string _newActionCtxs = string.Empty;

    private static readonly Vector4 ColMuted = new(0.60f, 0.60f, 0.58f, 1f);
    private static readonly Vector4 ColInfo = new(0.09f, 0.37f, 0.65f, 1f);
    private static readonly Vector4 ColDanger = new(0.64f, 0.17f, 0.17f, 1f);
    private static readonly Vector4 ColSuccess = new(0.06f, 0.43f, 0.34f, 1f);

    public QuickbarWindow(Plugin plugin)
        : base("STS — Raccourcis##sts_quickbar")
    {
        _plugin = plugin;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(160, 60),
            MaximumSize = new Vector2(800, 600),
        };
        Size = new Vector2(320, 120);
        SizeCondition = ImGuiCond.FirstUseEver;
        TriggerRefresh();
    }

    public void Dispose() { }

    public override void Draw()
    {
        Flags = _editMode
            ? ImGuiWindowFlags.None
            : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        var active = _activeCharacter;

        if (active is null)
        {
            ImGui.TextColored(ColMuted, "Aucun personnage actif.");
            return;
        }

        if (_editMode)
            DrawEditMode(active);
        else
            DrawNormalMode(active);
    }

    // ── Cache ──────────────────────────────────────────────────────────────────
    private Character? _activeCharacter;
    private Task? _refreshTask;

    public void TriggerRefresh()
    {
        if (_refreshTask is { IsCompleted: false }) return;
        _refreshTask = Task.Run(async () =>
        {
            try
            {
                var all = await _plugin.GetAllCharacters.ExecuteAsync();
                var activeId = _plugin.Configuration.ActiveCharacterId;
                _activeCharacter = activeId.HasValue
                    ? all.FirstOrDefault(c => c.Id == activeId.Value)
                    : null;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning("[STS] QuickbarWindow.TriggerRefresh échoué — {0}", ex.Message);
            }
        });
    }

    // ------------------------------------------------------------------ Mode normal

    private void DrawNormalMode(Character active)
    {
        var actions = _plugin.GetActionsForCharacter.Execute(active);

        if (actions.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucune action. Cliquez sur ✎ pour en configurer.");
        }
        else
        {
            foreach (var action in actions)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);

                if (ImGui.Button(action.Name + "##qb_" + action.Id))
                    _plugin.StartRoll(action);

                ImGui.PopStyleColor(3);

                if (ImGui.IsItemHovered() && action.Contexts.Count > 0)
                    ImGui.SetTooltip("Contextes : " + string.Join(", ", action.Contexts));

                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
        ImGui.Text(active.Name);
        ImGui.PopStyleColor();
        ImGui.SameLine();

        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 20);
        if (ImGui.Button("✎##qb_edit"))
            _editMode = true;
    }

    // ------------------------------------------------------------------ Mode édition

    private void DrawEditMode(Character active)
    {
        var allActions = _plugin.GetActionsForCharacter.GetAll(active);

        ImGui.TextColored(ColMuted, "ACTIONS VISIBLES DANS LA BARRE");
        ImGui.TextColored(ColMuted, "(décochez pour masquer, cochez pour afficher)");
        ImGui.Spacing();

        foreach (var action in allActions)
        {
            var isSelected = active.QuickbarActionIds.Count == 0
                || active.QuickbarActionIds.Contains(action.Id);

            if (ImGui.Checkbox("##sel_" + action.Id, ref isSelected))
            {
                if (isSelected)
                {
                    if (active.QuickbarActionIds.Count == 0)
                        active.QuickbarActionIds.AddRange(allActions.Select(a => a.Id));

                    if (!active.QuickbarActionIds.Contains(action.Id))
                        active.QuickbarActionIds.Add(action.Id);
                }
                else
                {
                    if (active.QuickbarActionIds.Count == 0)
                        active.QuickbarActionIds.AddRange(allActions.Select(a => a.Id));

                    active.QuickbarActionIds.Remove(action.Id);
                }

                if (active.QuickbarActionIds.Count == allActions.Count)
                    active.QuickbarActionIds.Clear();

                _ = Task.Run(() => _plugin.UpdateCharacter.ExecuteAsync(active));
            }

            ImGui.SameLine();

            // Nom — grisé si prédéfini
            if (action.IsPredefined)
                ImGui.TextColored(ColMuted, action.Name);
            else
                ImGui.Text(action.Name);

            // Bouton supprimer pour les customs
            if (!action.IsPredefined)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##del_{action.Id}"))
                {
                    active.QuickbarActionIds.Remove(action.Id);
                    _ = Task.Run(() => _plugin.DeleteCustomAction.ExecuteAsync(active, action.Id));
                }
                ImGui.PopStyleColor(3);
            }

            if (ImGui.IsItemHovered() && action.Contexts.Count > 0)
                ImGui.SetTooltip("Contextes : " + string.Join(", ", action.Contexts));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Formulaire création action custom
        ImGui.TextColored(ColMuted, "Nouvelle action personnalisée :");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(130);
        ImGui.InputText("Nom##qb_name", ref _newActionName, 64);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        ImGui.InputText("Contextes##qb_ctx", ref _newActionCtxs, 256);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Contextes séparés par des virgules.\nEx : attaque, attaque_magique");

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Contextes disponibles##qb_ctx_list"))
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
            ImGui.TextWrapped("Cliquez sur + pour ajouter au champ ci-dessus.");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            var allContexts = _plugin.ActionRepository.GetAll()
                .SelectMany(a => a.Contexts)
                .Concat(
                    _plugin.TraitRepository.GetAll()
                        .Where(t => t.Effects != null)
                        .SelectMany(t => t.Effects!)
                        .Where(e => e.Context != null)
                        .Select(e => e.Context!))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var ctx in allContexts)
            {
                if (ImGui.SmallButton("+##addctx_" + ctx))
                {
                    _newActionCtxs = string.IsNullOrWhiteSpace(_newActionCtxs)
                        ? ctx
                        : _newActionCtxs.TrimEnd() + ", " + ctx;
                }
                ImGui.SameLine();
                ImGui.TextColored(ColMuted, ctx);
            }
            ImGui.Spacing();
        }

        ImGui.Spacing();

        var canCreate = !string.IsNullOrWhiteSpace(_newActionName);

        if (!canCreate)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
        }

        if (ImGui.Button("+ Créer##qb_create") && canCreate)
        {
            var contexts = _newActionCtxs
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var actionName = _newActionName;
            _newActionName = string.Empty;
            _newActionCtxs = string.Empty;

            _ = Task.Run(async () =>
            {
                var created = await _plugin.CreateCustomAction.ExecuteAsync(active, actionName, contexts);
                if (active.QuickbarActionIds.Count > 0)
                    active.QuickbarActionIds.Add(created.Id);
            });
        }

        if (!canCreate) ImGui.PopStyleColor(2);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.35f));
        if (ImGui.Button("✓ Fermer##qb_close"))
            _editMode = false;
        ImGui.PopStyleColor(2);
    }
}
