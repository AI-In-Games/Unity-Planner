using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using AIInGames.Planning.Unity;
using AIInGames.Planning.Unity.Editor.Services;
using AIInGames.Planning.Unity.Editor.Extensions;
using AIInGames.Planning.Unity.Editor.DomainValidation;
using System.Collections.Generic;
using System.Linq;
#if ENABLE_VALIDATION
using AIInGames.Planning.PDDL.Validation;
#endif

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// Main domain editor window. Actions are the primary concept;
    /// predicates and types are secondary and collapsed by default.
    /// </summary>
    public class DomainEditorWindow : EditorWindow
    {
        private DomainAsset currentDomain;

        private ITypeValidator _typeValidator;
        private ITypeUsageAnalyzer _typeUsageAnalyzer;
        private DomainValidator _domainValidator;
        private ActionDefinition _currentAction;

        private TreeView typesTreeView;
        private ListView actionsListView;
        private ListView predicatesListView;
        private VisualElement mainEditorPanel;
        private Label domainNameLabel;
        private Foldout typesFoldout;
        private Foldout predicatesFoldout;
        private VisualElement emptyActionsHint;

        private readonly List<ActionDefinition> visibleActions = new List<ActionDefinition>();
        private string actionSearch = string.Empty;
        private TextField actionsSearchField;

        private Foldout validationFoldout;
        private ScrollView validationList;
        private string lastValidationSignature;


        [MenuItem("Window/AI Planning/Domain Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DomainEditorWindow>();
            window.titleContent = new GUIContent("Domain Editor");
            window.minSize = new Vector2(820, 600);
        }

        public static void OpenWindow(DomainAsset domain)
        {
            var window = GetWindow<DomainEditorWindow>();
            window.titleContent = new GUIContent($"Domain Editor - {domain.DomainName}");
            window.SetDomain(domain);
        }

        public void SetDomain(DomainAsset domain)
        {
            currentDomain = domain;
            InitializeServices();
            RefreshUI();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        // Unity reverts the domain asset in place; reflect it in the UI so an undo of a rename, add, or
        // delete is visible immediately rather than after the next interaction.
        private void OnUndoRedo()
        {
            if (currentDomain == null) return;
            RefreshUI();
            if (_currentAction != null && currentDomain.Actions.Contains(_currentAction))
                ShowActionEditor(_currentAction);
            else
            {
                _currentAction = null;
                mainEditorPanel?.Clear();
            }
        }

        private void InitializeServices()
        {
            _typeValidator     = new TypeValidator();
            _typeUsageAnalyzer = new TypeUsageAnalyzer();
            _domainValidator   = new DomainValidator();
        }

        private void CreateGUI()
        {
            InitializeServices();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/DomainEditorWindow");
            if (visualTree == null)
            {
                Debug.LogError("Failed to load DomainEditorWindow.uxml");
                return;
            }
            visualTree.CloneTree(rootVisualElement);

            var stylesheet = Resources.Load<StyleSheet>("USS/DomainEditorStyles");
            if (stylesheet != null)
                rootVisualElement.styleSheets.Add(stylesheet);

            BindUIReferences();
            SetupEventHandlers();

            // The domain is a ScriptableObject with no change event, and edits happen across many
            // scattered controls, so poll the validator on a light cadence. RefreshValidation only
            // rebuilds the panel when the set of issues actually changes.
            rootVisualElement.schedule.Execute(RefreshValidation).Every(500);

            if (currentDomain != null)
                RefreshUI();
        }

        // ── Binding ────────────────────────────────────────────────────────────

        private void BindUIReferences()
        {
            domainNameLabel  = rootVisualElement.Q<Label>("domain-name-label");
            mainEditorPanel  = rootVisualElement.Q<VisualElement>("main-editor-panel");
            emptyActionsHint = rootVisualElement.Q<VisualElement>("empty-actions-hint");

            typesFoldout = rootVisualElement.Q<Foldout>("types-foldout");
            predicatesFoldout = rootVisualElement.Q<Foldout>("predicates-foldout");

            validationFoldout = rootVisualElement.Q<Foldout>("validation-foldout");
            validationList = rootVisualElement.Q<ScrollView>("validation-list");

            BindTypesTree();
            BindPredicatesList();
            BindActionsList();
            ApplyTooltips();
        }

        private void ApplyTooltips()
        {
            rootVisualElement.Q<Button>("validate-button").tooltip      = DomainEditorStrings.Tooltips.Validate;
            rootVisualElement.Q<Button>("pddl-button").tooltip          = DomainEditorStrings.Tooltips.Pddl;
            rootVisualElement.Q<Button>("add-action-button").tooltip    = DomainEditorStrings.Tooltips.AddAction;
            rootVisualElement.Q<Button>("add-predicate-button").tooltip = DomainEditorStrings.Tooltips.AddPredicate;
            rootVisualElement.Q<Button>("add-type-button").tooltip      = DomainEditorStrings.Tooltips.AddType;
            rootVisualElement.Q<VisualElement>("actions-section").tooltip = DomainEditorStrings.Tooltips.ActionsSection;
            if (predicatesFoldout != null) predicatesFoldout.tooltip    = DomainEditorStrings.Tooltips.PredicatesFoldout;
            if (typesFoldout != null)      typesFoldout.tooltip         = DomainEditorStrings.Tooltips.TypesFoldout;
        }

        private void BindTypesTree()
        {
            typesTreeView = rootVisualElement.Q<TreeView>("types-tree");
            if (typesTreeView == null) return;

            typesTreeView.selectionType = SelectionType.Single;
            typesTreeView.makeItem = () => new Label();
            typesTreeView.bindItem = (element, index) =>
            {
                var item = typesTreeView.GetItemDataForIndex<TypeDefinition>(index);
                (element as Label).text = item.TypeName;
            };
        }

        private void BindPredicatesList()
        {
            predicatesListView = rootVisualElement.Q<ListView>("predicates-list");
            if (predicatesListView == null) return;

            predicatesListView.selectionType = SelectionType.Single;
            predicatesListView.makeItem = () => new Label();
            predicatesListView.bindItem = (element, index) =>
            {
                if (currentDomain == null || index >= currentDomain.Predicates.Count) return;
                var pred = currentDomain.Predicates[index];
                var label = element as Label;
                label.text = pred.Parameters.Count > 0
                    ? $"{pred.PredicateName}({string.Join(", ", pred.Parameters.Select(p => p.ParameterName.TrimStart('?')))})"
                    : pred.PredicateName;
            };
        }

        private void BindActionsList()
        {
            actionsListView = rootVisualElement.Q<ListView>("actions-list");
            if (actionsListView == null) return;

            actionsListView.selectionType = SelectionType.Single;
            actionsListView.itemsSource = visibleActions;
            actionsListView.makeItem = () =>
            {
                var item = new VisualElement();
                item.AddToClassList("action-list-item");
                var name = new Label();
                name.name = "action-name";
                name.AddToClassList("action-list-name");
                var stats = new Label();
                stats.name = "action-stats";
                stats.AddToClassList("action-list-stats");
                item.Add(name);
                item.Add(stats);
                return item;
            };
            actionsListView.bindItem = (element, index) =>
            {
                if (index < 0 || index >= visibleActions.Count) return;
                var action = visibleActions[index];
                element.Q<Label>("action-name").text = action.ActionName;
                int p = action.Parameters.Count;
                int r = action.Preconditions?.Conditions?.Count ?? 0;
                int e = action.Effects?.Effects?.Count ?? 0;
                element.Q<Label>("action-stats").text = $"{p}p · {r}r · {e}e";
            };
        }

        private void SetupEventHandlers()
        {
            rootVisualElement.Q<Button>("pddl-button")?.RegisterCallback<ClickEvent>(evt => ShowPddlMenu());
            rootVisualElement.Q<Button>("validate-button")?.RegisterCallback<ClickEvent>(evt => ValidateDomain());

            rootVisualElement.Q<Button>("add-action-button")?.RegisterCallback<ClickEvent>(evt => AddAction());
            rootVisualElement.Q<Button>("add-predicate-button")?.RegisterCallback<ClickEvent>(evt => AddPredicate());
            rootVisualElement.Q<Button>("add-type-button")?.RegisterCallback<ClickEvent>(evt => AddType());

            if (typesTreeView != null)
                typesTreeView.selectionChanged += OnTypeSelected;
            if (predicatesListView != null)
                predicatesListView.selectionChanged += OnPredicateSelected;
            if (actionsListView != null)
                actionsListView.selectionChanged += OnActionSelected;

            actionsSearchField = rootVisualElement.Q<TextField>("actions-search");
            if (actionsSearchField != null)
                actionsSearchField.RegisterValueChangedCallback(evt =>
                {
                    actionSearch = evt.newValue ?? string.Empty;
                    RefreshActionsList();
                });
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (currentDomain == null) return;

            if (domainNameLabel != null)
                domainNameLabel.text = $"Domain: {currentDomain.DomainName}";

            if (predicatesFoldout != null)
                predicatesFoldout.text = string.Format(DomainEditorStrings.Labels.PredicatesFoldoutFormat, currentDomain.Predicates.Count);
            if (typesFoldout != null)
                typesFoldout.text = string.Format(DomainEditorStrings.Labels.TypesFoldoutFormat, currentDomain.Types.Count);

            RefreshTypesTree();

            if (predicatesListView != null)
            {
                predicatesListView.itemsSource = currentDomain.Predicates;
                predicatesListView.Rebuild();
            }

            RefreshActionsList();
            RefreshValidation();
        }

        // Rebuilds the action list from the domain, filtered by the search box. The empty hint tracks
        // whether the domain has any actions at all, not whether the current filter matched.
        private void RefreshActionsList()
        {
            if (actionsListView == null) return;

            visibleActions.Clear();
            if (currentDomain != null)
            {
                string query = actionSearch?.Trim() ?? string.Empty;
                foreach (var action in currentDomain.Actions)
                {
                    if (query.Length == 0 ||
                        action.ActionName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        visibleActions.Add(action);
                }
            }

            actionsListView.itemsSource = visibleActions;
            actionsListView.Rebuild();

            bool hasActions = currentDomain != null && currentDomain.Actions.Count > 0;
            actionsListView.style.display = hasActions ? DisplayStyle.Flex : DisplayStyle.None;
            if (emptyActionsHint != null)
                emptyActionsHint.style.display = hasActions ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Type Hierarchy ─────────────────────────────────────────────────────

        private void RefreshTypesTree()
        {
            if (typesTreeView == null || currentDomain == null) return;

            // Build the hierarchy with the pure, unit-tested builder, then show the declared types
            // nested by parent. "object" is the implicit PDDL root and is not shown as a node.
            TypeHierarchyNode root = TypeHierarchyBuilder.Build(currentDomain.Types);

            var rootItems = new List<TreeViewItemData<TypeDefinition>>();
            foreach (var child in root.Children)
                rootItems.Add(ToTreeItem(child));

            typesTreeView.SetRootItems(rootItems);
            typesTreeView.Rebuild();
            typesTreeView.ExpandAll();
        }

        private static TreeViewItemData<TypeDefinition> ToTreeItem(TypeHierarchyNode node)
        {
            if (node.Children.Count == 0)
                return new TreeViewItemData<TypeDefinition>(node.Type.GetHashCode(), node.Type);

            var children = new List<TreeViewItemData<TypeDefinition>>();
            foreach (var child in node.Children)
                children.Add(ToTreeItem(child));
            return new TreeViewItemData<TypeDefinition>(node.Type.GetHashCode(), node.Type, children);
        }

        // ── Selection ──────────────────────────────────────────────────────────

        private void OnTypeSelected(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is TypeDefinition t)
                {
                    actionsListView?.ClearSelection();
                    predicatesListView?.ClearSelection();
                    ShowTypeEditor(t);
                    break;
                }
            }
        }

        private void OnPredicateSelected(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is PredicateDefinition p)
                {
                    actionsListView?.ClearSelection();
                    typesTreeView?.SetSelection(new int[0]);
                    ShowPredicateEditor(p);
                    break;
                }
            }
        }

        private void OnActionSelected(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is ActionDefinition a)
                {
                    predicatesListView?.ClearSelection();
                    typesTreeView?.SetSelection(new int[0]);
                    ShowActionEditor(a);
                    break;
                }
            }
        }

        // ── Type Editor ────────────────────────────────────────────────────────

        private void ShowTypeEditor(TypeDefinition type)
        {
            if (mainEditorPanel == null) return;
            mainEditorPanel.Clear();

            var template = Resources.Load<VisualTreeAsset>("UXML/TypeEditor");
            if (template == null) return;
            template.CloneTree(mainEditorPanel);

            var nameField = mainEditorPanel.Q<TextField>("type-name");
            if (nameField != null)
            {
                nameField.value = type.TypeName;
                nameField.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, $"Rename Type"))
                        type.TypeName = evt.newValue;
                    RefreshTypesTree();
                });
            }

            var parentDropdown = mainEditorPanel.Q<DropdownField>("parent-type");
            if (parentDropdown != null)
            {
                var choices = new List<string> { "object" };
                foreach (var t in currentDomain.Types)
                    if (t.TypeName != type.TypeName) choices.Add(t.TypeName);

                parentDropdown.choices = choices;
                parentDropdown.value = type.ParentType ?? "object";
                parentDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (_typeValidator.WouldCreateCircularDependency(currentDomain, type.TypeName, evt.newValue))
                    {
                        EditorUtility.DisplayDialog("Invalid Parent",
                            $"Setting '{evt.newValue}' as parent would create a circular dependency.", "OK");
                        parentDropdown.SetValueWithoutNotify(type.ParentType ?? "object");
                        return;
                    }
                    currentDomain.ChangeTypeParent(type, evt.newValue);
                    EditorApplication.delayCall += () => { if (currentDomain != null) RefreshTypesTree(); };
                });
            }

            var childrenLabel = mainEditorPanel.Q<Label>("children-list");
            if (childrenLabel != null)
            {
                var children = currentDomain.Types.Where(t => t.ParentType == type.TypeName).ToList();
                childrenLabel.text = children.Count > 0 ? string.Join(", ", children.Select(t => t.TypeName)) : "(none)";
                if (children.Count == 0) childrenLabel.AddToClassList("placeholder-text");
            }

            mainEditorPanel.Q<Button>("delete-type-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                var usages = _typeUsageAnalyzer.FindTypeUsages(currentDomain, type.TypeName);
                if (usages.Count > 0)
                {
                    EditorUtility.DisplayDialog("Cannot Delete",
                        $"'{type.TypeName}' is used in:\n\n{string.Join("\n", usages)}\n\nRemove these usages first.", "OK");
                    return;
                }
                if (EditorUtility.DisplayDialog("Delete Type", $"Delete type '{type.TypeName}'?", "Delete", "Cancel"))
                {
                    using (new UndoScope(currentDomain, "Delete Type"))
                        currentDomain.RemoveType(type);
                    RefreshUI();
                    mainEditorPanel.Clear();
                }
            });
        }

        // ── Predicate Editor ───────────────────────────────────────────────────

        private void ShowPredicateEditor(PredicateDefinition predicate)
        {
            if (mainEditorPanel == null) return;
            mainEditorPanel.Clear();

            var template = Resources.Load<VisualTreeAsset>("UXML/PredicateEditor");
            if (template == null) return;
            template.CloneTree(mainEditorPanel);

            var nameField = mainEditorPanel.Q<TextField>("predicate-name");
            if (nameField != null)
            {
                nameField.value = predicate.PredicateName;
                nameField.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Rename Predicate"))
                        predicate.PredicateName = evt.newValue;
                    predicatesListView?.Rebuild();
                });
            }

            BuildPredicateParametersUI(predicate);

            mainEditorPanel.Q<Button>("add-parameter-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                using (new UndoScope(currentDomain, "Add Parameter"))
                    predicate.Parameters.Add(new PredicateParameter("?param", "object"));
                BuildPredicateParametersUI(predicate);
                predicatesListView?.Rebuild();
            });

            mainEditorPanel.Q<Button>("delete-predicate-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                if (EditorUtility.DisplayDialog("Delete Predicate",
                    $"Delete '{predicate.PredicateName}'?", "Delete", "Cancel"))
                {
                    using (new UndoScope(currentDomain, "Delete Predicate"))
                        currentDomain.RemovePredicate(predicate);
                    RefreshUI();
                    mainEditorPanel.Clear();
                }
            });
        }

        private void BuildPredicateParametersUI(PredicateDefinition predicate)
        {
            var container = mainEditorPanel?.Q<VisualElement>("parameters-container");
            if (container == null) return;
            container.Clear();

            for (int i = 0; i < predicate.Parameters.Count; i++)
            {
                var param = predicate.Parameters[i];
                int capturedIndex = i;

                var row = new VisualElement();
                row.AddToClassList("parameter-row");

                var typeBtn = MakeTypeMenuButton(
                    () => param.ParameterType,
                    t =>
                    {
                        using (new UndoScope(currentDomain, "Change Parameter Type"))
                            param.ParameterType = t;
                    },
                    name => { param.ParameterType = name; predicatesListView?.Rebuild(); });
                row.Add(typeBtn);

                var nameField = new TextField();
                nameField.AddToClassList("param-name-field");
                nameField.SetValueWithoutNotify(param.ParameterName.TrimStart('?'));
                nameField.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Edit Parameter Name"))
                        param.ParameterName = evt.newValue.StartsWith("?") ? evt.newValue : "?" + evt.newValue;
                });
                row.Add(nameField);

                var deleteBtn = new Button { text = "×" };
                deleteBtn.AddToClassList("row-delete-button");
                deleteBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    using (new UndoScope(currentDomain, "Delete Parameter"))
                        predicate.Parameters.RemoveAt(capturedIndex);
                    BuildPredicateParametersUI(predicate);
                    predicatesListView?.Rebuild();
                });
                row.Add(deleteBtn);

                container.Add(row);
            }
        }

        // ── Action Editor ──────────────────────────────────────────────────────

        private void ShowActionEditor(ActionDefinition action)
        {
            if (mainEditorPanel == null) return;
            mainEditorPanel.Clear();
            _currentAction = action;

            var template = Resources.Load<VisualTreeAsset>("UXML/ActionEditor");
            if (template == null) return;
            template.CloneTree(mainEditorPanel);

            var nameField = mainEditorPanel.Q<TextField>("action-name");
            if (nameField != null)
            {
                nameField.value = action.ActionName;
                nameField.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Rename Action"))
                        action.ActionName = evt.newValue;
                    actionsListView?.Rebuild();
                    RefreshValidation();
                });
            }

            ApplyActionEditorTooltips();

            BuildParametersUI(action);
            mainEditorPanel.Q<Button>("add-parameter-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                using (new UndoScope(currentDomain, "Add Parameter"))
                    action.Parameters.Add(new ActionParameter("?param", "object"));
                BuildParametersUI(action);
                actionsListView?.Rebuild();
            });

            BuildConditionsUI(action);
            mainEditorPanel.Q<Button>("add-precondition-button")?.RegisterCallback<ClickEvent>(evt =>
                ShowAddConditionMenu(action.Preconditions.Conditions, action, depth: 0));

            BuildEffectsUI(action);
            mainEditorPanel.Q<Button>("add-effect-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                using (new UndoScope(currentDomain, "Add Effect"))
                    action.Effects.Effects.Add(new Effect());
                BuildEffectsUI(action);
                actionsListView?.Rebuild();
            });

            mainEditorPanel.Q<Button>("delete-action-button")?.RegisterCallback<ClickEvent>(evt =>
            {
                if (EditorUtility.DisplayDialog("Delete Action",
                    $"Delete action '{action.ActionName}'?", "Delete", "Cancel"))
                {
                    using (new UndoScope(currentDomain, "Delete Action"))
                        currentDomain.RemoveAction(action);
                    RefreshUI();
                    mainEditorPanel.Clear();
                }
            });
        }

        private void ApplyActionEditorTooltips()
        {
            if (mainEditorPanel == null) return;
            var paramSection = mainEditorPanel.Q("param-section");
            if (paramSection != null) paramSection.tooltip = DomainEditorStrings.Tooltips.ParametersSection;

            var preconditionSection = mainEditorPanel.Q("precondition-section");
            if (preconditionSection != null) preconditionSection.tooltip = DomainEditorStrings.Tooltips.PreconditionsSection;

            var effectSection = mainEditorPanel.Q("effect-section");
            if (effectSection != null) effectSection.tooltip = DomainEditorStrings.Tooltips.EffectsSection;

            var addParamBtn = mainEditorPanel.Q<Button>("add-parameter-button");
            if (addParamBtn != null) addParamBtn.tooltip = DomainEditorStrings.Tooltips.AddParameter;

            var addPreconditionBtn = mainEditorPanel.Q<Button>("add-precondition-button");
            if (addPreconditionBtn != null) addPreconditionBtn.tooltip = DomainEditorStrings.Tooltips.AddPrecondition;

            var addEffectBtn = mainEditorPanel.Q<Button>("add-effect-button");
            if (addEffectBtn != null) addEffectBtn.tooltip = DomainEditorStrings.Tooltips.AddEffect;
        }

        // ── Validation Panel (bottom, collapsible) ───────────────────────────────

        // Recomputes the domain's issues and, only when they changed, rebuilds the bottom panel. Safe
        // to call every frame; the signature guard avoids rebuilding (and flicker) when nothing changed.
        private void RefreshValidation()
        {
            if (validationList == null || validationFoldout == null) return;

            var issues = new List<DomainIssue>();
            if (currentDomain != null && _domainValidator != null)
                issues.AddRange(_domainValidator.Validate(currentDomain));

            string signature = BuildValidationSignature(issues);
            if (signature == lastValidationSignature) return;
            lastValidationSignature = signature;

            validationList.Clear();
            int errors = 0, warnings = 0;
            foreach (var issue in issues)
            {
                if (issue.Severity == IssueSeverity.Error) errors++; else warnings++;
                validationList.Add(BuildValidationRow(issue));
            }

            validationFoldout.text = issues.Count == 0
                ? "Problems: none"
                : $"Problems: {errors} error(s), {warnings} warning(s)";
        }

        private VisualElement BuildValidationRow(DomainIssue issue)
        {
            var row = new VisualElement();
            row.AddToClassList("validation-issue");
            row.AddToClassList(issue.Severity == IssueSeverity.Error ? "validation-error" : "validation-warning");

            var severityLabel = new Label(issue.Severity == IssueSeverity.Error ? "ERROR" : "WARN");
            severityLabel.AddToClassList("validation-severity");
            row.Add(severityLabel);

            string text = issue.ActionName != null ? $"[{issue.ActionName}] {issue.Message}" : issue.Message;
            var message = new Label(text);
            message.AddToClassList("validation-message");
            row.Add(message);

            // Clicking an action issue selects that action so the user can fix it.
            if (issue.ActionName != null)
                row.RegisterCallback<ClickEvent>(_ => SelectActionByName(issue.ActionName));

            return row;
        }

        private void SelectActionByName(string actionName)
        {
            if (currentDomain == null) return;
            for (int i = 0; i < visibleActions.Count; i++)
            {
                if (visibleActions[i].ActionName == actionName)
                {
                    actionsListView?.SetSelection(i);
                    return;
                }
            }
        }

        private static string BuildValidationSignature(List<DomainIssue> issues)
        {
            if (issues.Count == 0) return string.Empty;
            var parts = new string[issues.Count];
            for (int i = 0; i < issues.Count; i++)
                parts[i] = $"{issues[i].Severity}|{issues[i].ActionName}|{issues[i].Message}";
            return string.Join("\n", parts);
        }

        // ── Parameters UI ──────────────────────────────────────────────────────

        private void BuildParametersUI(ActionDefinition action)
        {
            var container = mainEditorPanel?.Q<VisualElement>("parameters-container");
            if (container == null) return;
            container.Clear();

            for (int i = 0; i < action.Parameters.Count; i++)
            {
                var param = action.Parameters[i];
                int capturedIndex = i;

                var row = new VisualElement();
                row.AddToClassList("parameter-row");

                var typeBtn = MakeTypeMenuButton(
                    () => param.ParameterType,
                    t =>
                    {
                        using (new UndoScope(currentDomain, "Change Parameter Type"))
                            param.ParameterType = t;
                        BuildConditionsUI(action);
                        BuildEffectsUI(action);
                    },
                    name => { param.ParameterType = name; });
                row.Add(typeBtn);

                var nameField = new TextField();
                nameField.AddToClassList("param-name-field");
                nameField.SetValueWithoutNotify(param.ParameterName.TrimStart('?'));
                nameField.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Edit Parameter Name"))
                        param.ParameterName = evt.newValue.StartsWith("?") ? evt.newValue : "?" + evt.newValue;
                });
                row.Add(nameField);

                var deleteBtn = new Button { text = "×" };
                deleteBtn.AddToClassList("row-delete-button");
                deleteBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    using (new UndoScope(currentDomain, "Delete Parameter"))
                        action.Parameters.RemoveAt(capturedIndex);
                    BuildParametersUI(action);
                    BuildConditionsUI(action);
                    BuildEffectsUI(action);
                    actionsListView?.Rebuild();
                });
                row.Add(deleteBtn);

                container.Add(row);
            }

            RefreshValidation();
        }

        // ── Conditions UI (recursive tree) ─────────────────────────────────────

        // AND/OR groups can be nested at most this many levels deep (enforced by the UI menu).
        private const int MaxConditionNestingDepth = 3;

        private void BuildConditionsUI(ActionDefinition action)
        {
            var container = mainEditorPanel?.Q<VisualElement>("conditions-container");
            if (container == null) return;
            container.Clear();

            foreach (var node in action.Preconditions.Conditions)
                container.Add(BuildConditionNodeElement(node, action.Preconditions.Conditions, action, depth: 0));

            RefreshValidation();
        }

        private VisualElement BuildConditionNodeElement(
            ConditionNode node, List<ConditionNode> parentList, ActionDefinition action, int depth)
        {
            switch (node.Type)
            {
                case ConditionNode.NodeType.Predicate:
                case ConditionNode.NodeType.Not:
                    return BuildSingleConditionRow(node, parentList, action);
                case ConditionNode.NodeType.And:
                case ConditionNode.NodeType.Or:
                    return BuildGroupBox(node, parentList, action, depth);
                default:
                    var l = new Label($"[{node.Type}]");
                    l.AddToClassList("group-placeholder");
                    return l;
            }
        }

        /// <summary>
        /// Unified row for both positive (Predicate) and negated (Not) conditions.
        /// The IS/NOT toggle button lets the user flip between the two states in place.
        /// </summary>
        private VisualElement BuildSingleConditionRow(
            ConditionNode node, List<ConditionNode> parentList, ActionDefinition action)
        {
            bool isNegated = node.Type == ConditionNode.NodeType.Not;

            if (isNegated && node.Children.Count == 0)
                node.Children.Add(new ConditionNode { Type = ConditionNode.NodeType.Predicate });

            ConditionNode predicateNode = isNegated ? node.Children[0] : node;

            var rowRoot = new VisualElement();
            Resources.Load<VisualTreeAsset>("UXML/ConditionRow")?.CloneTree(rowRoot);

            var conditionEl = rowRoot.Q(className: "condition-row-flat") ?? rowRoot;
            if (isNegated) conditionEl.AddToClassList("condition-row-negated");

            var negateBtn = new Button();
            negateBtn.AddToClassList("condition-negate-button");
            negateBtn.tooltip = DomainEditorStrings.Tooltips.NegateCondition;
            SetNegateButton(negateBtn, isNegated);
            negateBtn.RegisterCallback<ClickEvent>(_ =>
            {
                int idx = parentList.IndexOf(node);
                if (idx < 0) return;
                using (new UndoScope(currentDomain, "Toggle Condition Negation"))
                {
                    if (isNegated)
                    {
                        parentList[idx] = predicateNode;
                    }
                    else
                    {
                        var notNode = new ConditionNode { Type = ConditionNode.NodeType.Not };
                        notNode.Children.Add(node);
                        parentList[idx] = notNode;
                    }
                }
                BuildConditionsUI(action);
            });
            conditionEl.Insert(0, negateBtn);

            BindPredicateDropdownInRow(rowRoot, predicateNode, action, () => BuildConditionsUI(action));

            rowRoot.Q<Button>("delete-button")?.RegisterCallback<ClickEvent>(_ =>
            {
                using (new UndoScope(currentDomain, "Delete Condition"))
                    parentList.Remove(node);
                BuildConditionsUI(action);
                actionsListView?.Rebuild();
            });

            return rowRoot;
        }

        private static void SetNegateButton(Button btn, bool isNegated)
        {
            btn.RemoveFromClassList("condition-negate--is");
            btn.RemoveFromClassList("condition-negate--not");
            if (isNegated)
            {
                btn.text = "NOT";
                btn.AddToClassList("condition-negate--not");
            }
            else
            {
                btn.text = "IS";
                btn.AddToClassList("condition-negate--is");
            }
        }

        private VisualElement BuildGroupBox(
            ConditionNode node, List<ConditionNode> parentList, ActionDefinition action, int depth)
        {
            bool isAnd = node.Type == ConditionNode.NodeType.And;

            var box = new VisualElement();
            box.AddToClassList("condition-group");
            box.AddToClassList(isAnd ? "and-group" : "or-group");

            var header = new VisualElement();
            header.AddToClassList("condition-group-header");

            var groupLabel = new Label(isAnd ? "AND" : "OR");
            groupLabel.AddToClassList(isAnd ? "and-label" : "or-label");
            header.Add(groupLabel);

            var addChildBtn = new Button { text = "+ Add" };
            addChildBtn.AddToClassList("section-add-button");
            addChildBtn.RegisterCallback<ClickEvent>(_ => ShowAddConditionMenu(node.Children, action, depth + 1));
            header.Add(addChildBtn);

            var deleteBtn = new Button { text = "×" };
            deleteBtn.AddToClassList("row-delete-button");
            deleteBtn.RegisterCallback<ClickEvent>(_ =>
            {
                using (new UndoScope(currentDomain, $"Delete {(isAnd ? "AND" : "OR")} Group"))
                    parentList.Remove(node);
                BuildConditionsUI(action);
                actionsListView?.Rebuild();
            });
            header.Add(deleteBtn);
            box.Add(header);

            var childrenContainer = new VisualElement();
            childrenContainer.AddToClassList("condition-group-children");
            foreach (var child in node.Children)
                childrenContainer.Add(BuildConditionNodeElement(child, node.Children, action, depth + 1));
            box.Add(childrenContainer);

            return box;
        }

        // ── Effects UI ─────────────────────────────────────────────────────────

        private void BuildEffectsUI(ActionDefinition action)
        {
            var container = mainEditorPanel?.Q<VisualElement>("effects-container");
            if (container == null) return;
            container.Clear();

            var rowTemplate = Resources.Load<VisualTreeAsset>("UXML/EffectRow");
            if (rowTemplate == null) return;

            for (int i = 0; i < action.Effects.Effects.Count; i++)
            {
                var effect = action.Effects.Effects[i];
                int capturedIndex = i;

                var effectRow = new VisualElement();
                rowTemplate.CloneTree(effectRow);

                var typeBtn = effectRow.Q<Button>("effect-type-button");
                if (typeBtn != null)
                {
                    SetEffectTypeButton(typeBtn, effect.Type == Effect.EffectType.Add);
                    typeBtn.RegisterCallback<ClickEvent>(_ =>
                    {
                        using (new UndoScope(currentDomain, "Toggle Effect Type"))
                            effect.Type = effect.Type == Effect.EffectType.Add
                                ? Effect.EffectType.Remove
                                : Effect.EffectType.Add;
                        SetEffectTypeButton(typeBtn, effect.Type == Effect.EffectType.Add);
                        RefreshValidation();
                    });
                }

                BindEffectPredicateDropdown(effectRow, effect, action);

                effectRow.Q<Button>("delete-button")?.RegisterCallback<ClickEvent>(_ =>
                {
                    using (new UndoScope(currentDomain, "Delete Effect"))
                        action.Effects.Effects.RemoveAt(capturedIndex);
                    BuildEffectsUI(action);
                    actionsListView?.Rebuild();
                });

                container.Add(effectRow);
            }

            RefreshValidation();
        }

        // ── Row Helpers ────────────────────────────────────────────────────────

        private void BindPredicateDropdownInRow(VisualElement row, ConditionNode predicateNode,
            ActionDefinition action, System.Action rebuildCallback)
        {
            var oldDropdown = row.Q<DropdownField>("predicate-dropdown");
            if (oldDropdown == null) return;

            var realNames = currentDomain.Predicates.Select(p => p.PredicateName).ToList();

            if (string.IsNullOrEmpty(predicateNode.PredicateName) && realNames.Count > 0)
            {
                using (new UndoScope(currentDomain, "Auto-select Predicate"))
                {
                    predicateNode.PredicateName = realNames[0];
                    SyncConditionArguments(predicateNode, action);
                }
            }

            var predBtn = MakePredicateMenuButton(
                () => predicateNode.PredicateName,
                name =>
                {
                    using (new UndoScope(currentDomain, "Change Condition Predicate"))
                    {
                        predicateNode.PredicateName = name;
                        SyncConditionArguments(predicateNode, action);
                    }
                    BuildArgsInConditionRow(row, predicateNode, action);
                },
                name =>
                {
                    predicateNode.PredicateName = name;
                    SyncConditionArguments(predicateNode, action);
                    BuildArgsInConditionRow(row, predicateNode, action);
                });

            oldDropdown.parent?.Insert(oldDropdown.parent.IndexOf(oldDropdown), predBtn);
            oldDropdown.RemoveFromHierarchy();

            BuildArgsInConditionRow(row, predicateNode, action);
        }

        private void BuildArgsInConditionRow(VisualElement row, ConditionNode node, ActionDefinition action)
        {
            var argsContainer = row.Q<VisualElement>("arguments-container");
            if (argsContainer == null) return;
            argsContainer.Clear();

            if (string.IsNullOrEmpty(node.PredicateName)) return;

            var predicate = currentDomain.Predicates.FirstOrDefault(p => p.PredicateName == node.PredicateName);
            if (predicate == null) return;

            SyncConditionArguments(node, action);

            for (int i = 0; i < predicate.Parameters.Count; i++)
            {
                int argIdx = i;
                string paramType = predicate.Parameters[i].ParameterType;
                var label = predicate.Parameters[i].ParameterName.TrimStart('?');

                var available = action.Parameters
                    .Where(p => p.ParameterType == paramType || paramType == "object")
                    .Select(p => p.ParameterName)
                    .ToList();

                if (available.Count == 0) available.Add("?");

                var argDropdown = new DropdownField($"{label}");
                argDropdown.choices = available;
                argDropdown.SetValueWithoutNotify(
                    available.Contains(node.Arguments[argIdx]) ? node.Arguments[argIdx] : available[0]);
                argDropdown.AddToClassList("arg-dropdown");

                argDropdown.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Change Argument"))
                        node.Arguments[argIdx] = evt.newValue;
                });

                argsContainer.Add(argDropdown);
            }
        }

        private void SyncConditionArguments(ConditionNode node, ActionDefinition action)
        {
            var predicate = currentDomain.Predicates.FirstOrDefault(p => p.PredicateName == node.PredicateName);
            if (predicate == null) return;

            while (node.Arguments.Count < predicate.Parameters.Count)
            {
                string paramType = predicate.Parameters[node.Arguments.Count].ParameterType;
                var match = action.Parameters.FirstOrDefault(p => p.ParameterType == paramType || paramType == "object");
                node.Arguments.Add(match?.ParameterName ?? "?");
            }
            while (node.Arguments.Count > predicate.Parameters.Count)
                node.Arguments.RemoveAt(node.Arguments.Count - 1);
        }

        private void BindEffectPredicateDropdown(VisualElement effectRow, Effect effect, ActionDefinition action)
        {
            var oldDropdown = effectRow.Q<DropdownField>("predicate-dropdown");
            if (oldDropdown == null) return;

            var realNames = currentDomain.Predicates.Select(p => p.PredicateName).ToList();

            if (string.IsNullOrEmpty(effect.PredicateName) && realNames.Count > 0)
            {
                using (new UndoScope(currentDomain, "Auto-select Predicate"))
                {
                    effect.PredicateName = realNames[0];
                    SyncEffectArguments(effect, action);
                }
            }

            var predBtn = MakePredicateMenuButton(
                () => effect.PredicateName,
                name =>
                {
                    using (new UndoScope(currentDomain, "Change Effect Predicate"))
                    {
                        effect.PredicateName = name;
                        SyncEffectArguments(effect, action);
                    }
                    BuildArgsInEffectRow(effectRow, effect, action);
                },
                name =>
                {
                    effect.PredicateName = name;
                    SyncEffectArguments(effect, action);
                    BuildArgsInEffectRow(effectRow, effect, action);
                });

            oldDropdown.parent?.Insert(oldDropdown.parent.IndexOf(oldDropdown), predBtn);
            oldDropdown.RemoveFromHierarchy();

            BuildArgsInEffectRow(effectRow, effect, action);
        }

        private void BuildArgsInEffectRow(VisualElement effectRow, Effect effect, ActionDefinition action)
        {
            var argsContainer = effectRow.Q<VisualElement>("arguments-container");
            if (argsContainer == null) return;
            argsContainer.Clear();

            if (string.IsNullOrEmpty(effect.PredicateName)) return;

            var predicate = currentDomain.Predicates.FirstOrDefault(p => p.PredicateName == effect.PredicateName);
            if (predicate == null) return;

            SyncEffectArguments(effect, action);

            for (int i = 0; i < predicate.Parameters.Count; i++)
            {
                int argIdx = i;
                string paramType = predicate.Parameters[i].ParameterType;
                var label = predicate.Parameters[i].ParameterName.TrimStart('?');

                var available = action.Parameters
                    .Where(p => p.ParameterType == paramType || paramType == "object")
                    .Select(p => p.ParameterName)
                    .ToList();

                if (available.Count == 0) available.Add("?");

                var argDropdown = new DropdownField($"{label}");
                argDropdown.choices = available;
                argDropdown.SetValueWithoutNotify(
                    available.Contains(effect.Arguments[argIdx]) ? effect.Arguments[argIdx] : available[0]);
                argDropdown.AddToClassList("arg-dropdown");

                argDropdown.RegisterValueChangedCallback(evt =>
                {
                    using (new UndoScope(currentDomain, "Change Argument"))
                        effect.Arguments[argIdx] = evt.newValue;
                });

                argsContainer.Add(argDropdown);
            }
        }

        private void SyncEffectArguments(Effect effect, ActionDefinition action)
        {
            var predicate = currentDomain.Predicates.FirstOrDefault(p => p.PredicateName == effect.PredicateName);
            if (predicate == null) return;

            while (effect.Arguments.Count < predicate.Parameters.Count)
            {
                string paramType = predicate.Parameters[effect.Arguments.Count].ParameterType;
                var match = action.Parameters.FirstOrDefault(p => p.ParameterType == paramType || paramType == "object");
                effect.Arguments.Add(match?.ParameterName ?? "?");
            }
            while (effect.Arguments.Count > predicate.Parameters.Count)
                effect.Arguments.RemoveAt(effect.Arguments.Count - 1);
        }

        private void SetEffectTypeButton(Button button, bool isAdd)
        {
            button.RemoveFromClassList("effect-type--add");
            button.RemoveFromClassList("effect-type--remove");
            if (isAdd)
            {
                button.text = "+ Add";
                button.AddToClassList("effect-type--add");
            }
            else
            {
                button.text = "- Remove";
                button.AddToClassList("effect-type--remove");
            }
        }

        private void ShowAddConditionMenu(List<ConditionNode> targetList, ActionDefinition action, int depth = 0)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Predicate"), false, () =>
            {
                using (new UndoScope(currentDomain, "Add Condition"))
                    targetList.Add(new ConditionNode { Type = ConditionNode.NodeType.Predicate });
                BuildConditionsUI(action);
                actionsListView?.Rebuild();
            });

            menu.AddItem(new GUIContent("NOT (negated predicate)"), false, () =>
            {
                var notNode = new ConditionNode { Type = ConditionNode.NodeType.Not };
                notNode.Children.Add(new ConditionNode { Type = ConditionNode.NodeType.Predicate });
                using (new UndoScope(currentDomain, "Add NOT Condition"))
                    targetList.Add(notNode);
                BuildConditionsUI(action);
                actionsListView?.Rebuild();
            });

            if (depth < MaxConditionNestingDepth)
            {
                menu.AddSeparator("");

                menu.AddItem(new GUIContent("AND group"), false, () =>
                {
                    using (new UndoScope(currentDomain, "Add AND Group"))
                        targetList.Add(new ConditionNode { Type = ConditionNode.NodeType.And });
                    BuildConditionsUI(action);
                    actionsListView?.Rebuild();
                });

                menu.AddItem(new GUIContent("OR group"), false, () =>
                {
                    using (new UndoScope(currentDomain, "Add OR Group"))
                        targetList.Add(new ConditionNode { Type = ConditionNode.NodeType.Or });
                    BuildConditionsUI(action);
                    actionsListView?.Rebuild();
                });
            }

            menu.ShowAsContext();
        }

        private void CreateTypeAndNavigate(System.Action<string> onCreated)
        {
            var name = GetUniqueName("new-type", n => currentDomain.Types.Any(t => t.TypeName == n));
            var newType = new TypeDefinition(name, "object");
            using (new UndoScope(currentDomain, "Add Type"))
                currentDomain.AddType(newType);
            if (typesFoldout != null) typesFoldout.value = true;
            RefreshUI();
            onCreated(name);
            // Defer selection until after the rebuild has settled.
            // SetSelection triggers selectionChanged → OnTypeSelected → ShowTypeEditor.
            int id = newType.GetHashCode();
            EditorApplication.delayCall += () =>
            {
                typesTreeView?.SetSelection(new[] { id });
                var nameField = mainEditorPanel?.Q<TextField>("type-name");
                nameField?.Focus();
                nameField?.SelectAll();
            };
        }

        private void CreatePredicateAndNavigate(System.Action<string> onCreated)
        {
            var name = GetUniqueName("new-predicate", n => currentDomain.Predicates.Any(p => p.PredicateName == n));
            var newPred = new PredicateDefinition(name);
            using (new UndoScope(currentDomain, "Add Predicate"))
                currentDomain.AddPredicate(newPred);
            if (predicatesFoldout != null) predicatesFoldout.value = true;
            RefreshUI();
            onCreated(name);
            int index = currentDomain.Predicates.Count - 1;
            EditorApplication.delayCall += () =>
            {
                predicatesListView?.SetSelection(index);
                predicatesListView?.ScrollToItem(index);
                var nameField = mainEditorPanel?.Q<TextField>("predicate-name");
                nameField?.Focus();
                nameField?.SelectAll();
            };
        }

        private string GetUniqueName(string baseName, System.Func<string, bool> exists)
        {
            if (!exists(baseName)) return baseName;
            int i = 1;
            while (exists($"{baseName}-{i}")) i++;
            return $"{baseName}-{i}";
        }

        // ── Navigation Actions ─────────────────────────────────────────────────

        private void AddAction()
        {
            if (currentDomain == null) return;
            var newAction = new ActionDefinition("new-action");
            using (new UndoScope(currentDomain, "Add Action"))
                currentDomain.AddAction(newAction);

            // Clear any active filter so the new action is visible, then select it.
            actionSearch = string.Empty;
            actionsSearchField?.SetValueWithoutNotify(string.Empty);

            RefreshUI();
            int newIndex = visibleActions.IndexOf(newAction);
            if (newIndex >= 0)
                actionsListView?.SetSelection(newIndex);
        }

        private void AddPredicate()
        {
            if (currentDomain == null) return;
            var newPredicate = new PredicateDefinition("new-predicate");
            using (new UndoScope(currentDomain, "Add Predicate"))
                currentDomain.AddPredicate(newPredicate);
            if (predicatesFoldout != null) predicatesFoldout.value = true;
            RefreshUI();
        }

        private void AddType()
        {
            if (currentDomain == null) return;
            var newType = new TypeDefinition("new-type", "object");
            using (new UndoScope(currentDomain, "Add Type"))
                currentDomain.AddType(newType);
            if (typesFoldout != null) typesFoldout.value = true;
            RefreshUI();
            typesTreeView?.SetSelection(new[] { newType.GetHashCode() });
        }

        // ── Utilities ──────────────────────────────────────────────────────────

        private List<string> GetAvailableTypes()
        {
            var types = new List<string> { "object" };
            if (currentDomain != null)
                types.AddRange(currentDomain.Types.Select(t => t.TypeName));
            return types;
        }

        // ── GenericMenu Dropdown Buttons ───────────────────────────────────────
        //
        // DropdownField.choices doesn't support separators. The Unity Editor
        // pattern for a dropdown with a separator is a Button + GenericMenu.

        private Button MakeTypeMenuButton(
            System.Func<string> getCurrent,
            System.Action<string> onSelect,
            System.Action<string> onCreated)
        {
            var btn = MakeMenuButton(getCurrent());
            btn.AddToClassList("param-type-dropdown");
            btn.RegisterCallback<ClickEvent>(_ =>
            {
                var current = getCurrent();
                var menu = new GenericMenu();
                foreach (var t in GetAvailableTypes())
                {
                    string captured = t;
                    menu.AddItem(new GUIContent(t), current == t, () =>
                    {
                        UpdateMenuButtonText(btn, captured);
                        onSelect(captured);
                    });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Create new type"), false, () =>
                    CreateTypeAndNavigate(name => { UpdateMenuButtonText(btn, name); onCreated(name); }));
                menu.ShowAsContext();
            });
            return btn;
        }

        private Button MakePredicateMenuButton(
            System.Func<string> getCurrent,
            System.Action<string> onSelect,
            System.Action<string> onCreated)
        {
            var btn = MakeMenuButton(getCurrent());
            btn.AddToClassList("predicate-dropdown");
            btn.RegisterCallback<ClickEvent>(_ =>
            {
                var current = getCurrent();
                var menu = new GenericMenu();
                if (currentDomain.Predicates.Count == 0)
                    menu.AddDisabledItem(new GUIContent("No predicates defined yet"));
                else
                    foreach (var pred in currentDomain.Predicates)
                    {
                        string name = pred.PredicateName;
                        menu.AddItem(new GUIContent(name), current == name, () =>
                        {
                            UpdateMenuButtonText(btn, name);
                            onSelect(name);
                        });
                    }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Create new predicate"), false, () =>
                    CreatePredicateAndNavigate(name => { UpdateMenuButtonText(btn, name); onCreated(name); }));
                menu.ShowAsContext();
            });
            return btn;
        }

        private static Button MakeMenuButton(string initialText)
        {
            var btn = new Button();
            btn.AddToClassList("custom-dropdown-button");
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.SpaceBetween;

            var textLabel = new Label(initialText);
            textLabel.name = "btn-value";
            textLabel.style.flexGrow = 1;
            textLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            textLabel.style.overflow = Overflow.Hidden;
            btn.Add(textLabel);

            var arrow = new Label("▾");
            arrow.AddToClassList("custom-dropdown-arrow");
            btn.Add(arrow);

            return btn;
        }

        private static void UpdateMenuButtonText(Button btn, string text)
        {
            var label = btn.Q<Label>("btn-value");
            if (label != null) label.text = text;
        }

        private void ShowPddlMenu()
        {
            if (currentDomain == null) return;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Preview (Console)"), false, () => DomainPddlActions.LogPreview(currentDomain));
            menu.AddItem(new GUIContent("Export to File..."), false, () => DomainPddlActions.Export(currentDomain));
            menu.ShowAsContext();
        }

        private void ValidateDomain()
        {
            if (currentDomain == null) return;

#if ENABLE_VALIDATION
            string domainName = currentDomain.DomainName;
            string domainPddl = DomainSerializer.ToPddl(currentDomain);
            string problemPddl = BuildMinimalProblem(currentDomain);

            ValidationResult result = ValPlanValidator.ValidateDomainAndProblem(domainPddl, problemPddl);

            if (result.BinaryMissing)
            {
                Debug.LogWarning($"[VAL] {domainName}: Validate binary not found in the PDDL parser package. " +
                    "Set PDDL_VALIDATE_PATH or build the binaries (see the parser package's Binaries/BUILD.md).");
                return;
            }

            if (result.IsValid)
                Debug.Log($"[VAL] {domainName}: all checks passed.\n{result.RawOutput}");
            else
            {
                foreach (string error in result.Errors)
                    Debug.LogError($"[VAL] {domainName}: {error}");
                Debug.LogError($"[VAL] {domainName}: full output:\n{result.RawOutput}");
            }
#else
            Debug.LogWarning("[VAL] Domain validation requires the ENABLE_VALIDATION scripting define, which enables " +
                "the VAL integration in the com.aiingames.pddl-parser package.");
#endif
        }

#if ENABLE_VALIDATION
        // A minimal problem that declares one dummy object per type, so VAL can type-check the action
        // parameters without a real problem instance.
        private static string BuildMinimalProblem(DomainAsset domain)
        {
            var objects = new System.Text.StringBuilder();
            foreach (var type in domain.Types)
                objects.Append($"  dummy-{type.TypeName} - {type.TypeName}\n");

            return $"(define (problem val-check)\n" +
                   $"  (:domain {domain.DomainName})\n" +
                   $"  (:objects\n{objects}  )\n" +
                   $"  (:init)\n" +
                   $"  (:goal (and))\n" +
                   $")";
        }
#endif
    }
}
