using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.WorldModel;
using FrooxEngine.UIX;
using FrooxEngine.Undo;
using HarmonyLib;
using NeosModLoader;
using System.Collections.Generic;

namespace KeepGlobalTransformToggle
{
    public class KeepGlobalTransformToggle : NeosMod
    {
        public override string Name => "KeepGlobalTransformToggle";

        public override string Author => "CatShark";

        public override string Version => "1.1";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.catshark.KeepGlobalTransformToggle");
            harmony.PatchAll();
        }
        [HarmonyPatch(typeof(SceneInspector),"OnChanges")]
        class SceneInspector_OnChanges_Patch
        {
            static void Postfix(SceneInspector __instance)
            {
                SyncRef<Slot> hierarchyContentRoot = (SyncRef<Slot>)AccessTools.Field(typeof(SceneInspector),"_hierarchyContentRoot").GetValue(__instance);
                if(hierarchyContentRoot.Target.ChildrenCount != 1)
                {
                    return;
                }
                Slot buttonRoot = hierarchyContentRoot.Target.AddSlot("KeepGlobalTransform Toggle");
                buttonRoot.AttachComponent<VerticalLayout>();
                buttonRoot.OrderOffset = -1;
                UIBuilder uIBuilder = new UIBuilder(buttonRoot);
                uIBuilder.Style.ForceExpandHeight = false;
                uIBuilder.Style.ChildAlignment = Alignment.TopLeft;
                uIBuilder.HorizontalLayout(4f);
                uIBuilder.Style.MinHeight = 32f;
                uIBuilder.Style.MinWidth = 32f;
                LocaleString label = "";
                FrooxEngine.UIX.Text text = uIBuilder.Text(in label, bestFit: true, Alignment.MiddleRight);
                Button button = text.Slot.AttachComponent<Button>();
                InteractionElement.ColorDriver colorDriver = button.ColorDrivers.Add();
                colorDriver.ColorDrive.Target = text.Color;
                colorDriver.NormalColor.Value = color.Black;
                colorDriver.HighlightColor.Value = color.Blue;
                colorDriver.PressColor.Value = color.Cyan;
                Slot userRoot = __instance.Slot.Parent;
                DynamicVariableSpace dynVarSpace = text.Slot.FindSpace("TransformTweaks");
                if (dynVarSpace == null)
                {
                    DynamicVariableSpace dvs = userRoot.AttachComponent<DynamicVariableSpace>();
                    dvs.SpaceName.Value = "TransformTweaks";
                    dynVarSpace = dvs;
                }
                DynamicValueVariable<bool> valueVariable = text.Slot.AttachComponent<DynamicValueVariable<bool>>();
                valueVariable.VariableName.Value = "TransformTweaks/keepGlobalTransform";
                valueVariable.Value.Value = true;
                ButtonToggle buttonToggle = text.Slot.AttachComponent<ButtonToggle>();
                buttonToggle.TargetValue.Target = valueVariable.Value;
                ValueOptionDescriptionDriver<bool> optionDriver = text.Slot.AttachComponent<ValueOptionDescriptionDriver<bool>>();
                optionDriver.Label.Target = text.Content;
                optionDriver.Value.Target = valueVariable.Value;
                optionDriver.DefaultOption.Label.Value = "Local";
                optionDriver.Options.Add();
                optionDriver.Options[0].Label.Value = "Global";
                optionDriver.Options[0].ReferenceValue.Value = true;

            }
        }

        [HarmonyPatch(typeof(SlotRecord), "TryReceive")]
        class SlotRecord_TryReceive_Patch
        {
            static bool Prefix(SlotRecord __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 point)
            {
                SyncRef<Slot> TargetSlot = __instance.TargetSlot;
                foreach (IGrabbable item in items)
                {
                    foreach (ReferenceProxy componentsInChild in item.Slot.GetComponentsInChildren<ReferenceProxy>())
                    {
                        Slot slot = componentsInChild.Reference.Target as Slot;
                        if (slot != null && slot != TargetSlot.Target)
                        {
                            slot.CreateTransformUndoState(parent: true);
                            __instance.Slot.FindSpace("TransformTweaks").TryReadValue<bool>("keepGlobalTransform",out bool val);
                            slot.SetParent(TargetSlot.Target, val);
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }

}
