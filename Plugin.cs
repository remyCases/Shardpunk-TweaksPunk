// Copyright (C) 2026 Rémy Cases
// See LICENSE file for extended copyright information.
// This file is part of the Speedshard repository from https://github.com/remyCases/Shardpunk-TweaksPunk.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;
using Shp.CharacterSystem;
using Shp.Core;
using Shp.GameActions;
using Shp.MissionSummary;
using Shp.StressSystem;
using Shp.WeaponSystem;
using UnityEngine;

namespace TweaksPunk;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        HarmonyFileLog.Enabled = true;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        CheckMethod(typeof(WeaponStats), "set_MaxHeat");
        CheckMethod(typeof(DirectionalStandingActionRoutine), "ActionToIdleTransitionRoutine");
        harmony.PatchAll();
    }
    private void CheckMethod(Type TargetType, string methodName, Type[] parameters = null)
    {
        MethodInfo method = AccessTools.Method(TargetType, methodName, parameters);
        if (method == null)
        {
            Logger.LogError($"{TargetType}.{methodName} method not found! Game version changed?");
            return;
        }
        Logger.LogInfo($"{TargetType}.{methodName} method found");
    }
    private void CheckMethod(Type TargetType, MethodType methodType, Type[] parameters = null)
    {
        switch (methodType)
        {
            case MethodType.Constructor:
                ConstructorInfo constructor = AccessTools.Constructor(TargetType, parameters);
                if (constructor == null)
                {
                    Logger.LogError($"{TargetType}.{methodType} method not found! Game version changed?");
                    return;
                }
                break;
            default:
                Logger.LogError($"Unknown methodType {TargetType}.{methodType}");
                return;
        }
        Logger.LogInfo($"{TargetType}.{methodType} method found");
    }
}

[HarmonyDebug]
[HarmonyPatch]
static class EnemiesPatch 
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WarCryRoutine))]
    [HarmonyPatch("Routine", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> WarCryRoutine(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {   
        int status = 0;

        Label toplabel = il.DefineLabel();
        Label endlabel = il.DefineLabel();
        Label endfinallylabel = il.DefineLabel();

        // previous switch has 7 cases, now 8
        Label[] jumpTable = [];
        int indexJumpTable = 0;
        bool found_ret = false;

        LocalBuilder local = il.DeclareLocal(typeof(IEnumerator<>).MakeGenericType(typeof(CombatCharacter)));

        FieldInfo current_field = typeof(WarCryRoutine)
            .GetNestedTypes(AccessTools.all).
            Where(t => t.Name.Contains("Routine")).
            SelectMany(AccessTools.GetDeclaredFields).
            FirstOrDefault(m => m.Name.Contains("current"));

        FieldInfo state_field = typeof(WarCryRoutine)
            .GetNestedTypes(AccessTools.all).
            Where(t => t.Name.Contains("Routine")).
            SelectMany(AccessTools.GetDeclaredFields).
            FirstOrDefault(m => m.Name.Contains("state"));

        foreach (CodeInstruction instruction in instructions)
        {
            // first we need to change the jumptable
            if(status == 0 && instruction.opcode == OpCodes.Switch) {
                status = 1;
                var tmp = new List<Label>();
                for(int i = 0 ; i < (instruction.operand as Label[]).Length  + 1 ; i++) tmp.Add(il.DefineLabel());
                jumpTable = tmp.ToArray();
                yield return new CodeInstruction(OpCodes.Switch, jumpTable);
                continue;
            }

            // first flag to be found
            if (status == 1 && instruction.Calls(AccessTools.Method(typeof(DirectionalStandingActionRoutine), "ActionToIdleTransitionRoutine")))
            {
                status = 2;
                yield return instruction;
                continue;
            }

            // second flag, next ldarg0 is the one
            if (status == 2 && instruction.opcode == OpCodes.Ldc_I4_1) {
                status = 3;
                yield return instruction;
                continue;
            } 

            // that the main changes
            if  (status == 3 && instruction.opcode == OpCodes.Ldarg_0) {
                // we wont change it anymore
                status = 4;

                // return the ldarg0 but change its label
                instruction.labels.Add(jumpTable[indexJumpTable]);
                indexJumpTable++;
                yield return instruction;

                // reset state
                yield return new CodeInstruction(OpCodes.Ldc_I4_M1, null);
                yield return new CodeInstruction(OpCodes.Stfld, state_field);

                // WaitForSecondsRealTime(0.5)
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.5f);
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(WaitForSecondsRealtime), new Type[] { typeof(float) }));

                // yield with current
                yield return new CodeInstruction(OpCodes.Stfld, current_field);
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                yield return new CodeInstruction(OpCodes.Ldc_I4_6, null);
                yield return new CodeInstruction(OpCodes.Stfld, state_field);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Ret, null);

                // reset state
                CodeInstruction afterRet = new CodeInstruction(OpCodes.Ldarg_0, null);
                
                afterRet.labels.Add(jumpTable[indexJumpTable]);
                indexJumpTable++;
                yield return afterRet;
                yield return new CodeInstruction(OpCodes.Ldc_I4_M1, null);
                yield return new CodeInstruction(OpCodes.Stfld, state_field);

                // get all alive characters
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(World), "get_Instance"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(World), "get_CharactersContainer"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CharactersContainer), "GetAlivePlayerCharacters"));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IEnumerable<>).MakeGenericType(typeof(CombatCharacter)), "GetEnumerator"));
                yield return new CodeInstruction(OpCodes.Stloc_S, local);
                
                // foreach loop
                CodeInstruction beginblock = new CodeInstruction(OpCodes.Br_S, endlabel);
                beginblock.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
                yield return beginblock;
                CodeInstruction toploc = new CodeInstruction(OpCodes.Ldloc_S, local);
                toploc.labels.Add(toplabel);
                yield return toploc;
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IEnumerator<>).MakeGenericType(typeof(CombatCharacter)), "get_Current"));

                // main operations
                yield return new CodeInstruction(OpCodes.Ldc_I4_2, null);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Panicker), "ChangeStressAndDisplayChangeTextInCombat"));

                // move next
                CodeInstruction endloc = new CodeInstruction(OpCodes.Ldloc_S, local);
                endloc.labels.Add(endlabel);
                yield return endloc;
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IEnumerator), "MoveNext"));
                yield return new CodeInstruction(OpCodes.Brtrue_S, toplabel);

                // finally block
                CodeInstruction beginfinally = new CodeInstruction(OpCodes.Ldloc_S, local);
                beginfinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
                yield return beginfinally;
                yield return new CodeInstruction(OpCodes.Brfalse_S, endfinallylabel);
                yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(IDisposable), "Dispose"));

                // end finally
                CodeInstruction endfinally = new CodeInstruction(OpCodes.Endfinally, null);
                endfinally.labels.Add(endfinallylabel);
                endfinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                yield return endfinally;

                // to restart where we were before
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                continue;
            }

            // ditch the three following instructions
            if(status >= 4 && status < 7) {
                status++;
                continue;
            }

            // we altered the jump table, we need to change the internal state also
            if(status == 7 && instruction.opcode == OpCodes.Ldc_I4_6) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_7, null);
                continue;
            }

            // since we change the jumpTable we need to update all jumpLabel
            // finding a ret is the first step
            if(status > 0 && instruction.opcode == OpCodes.Ret) {
                found_ret = true;
                yield return instruction;
                continue;
            }
            // change its label if the next one is a ldarg0 indeed
            if(found_ret) {
                found_ret = false;
                if (instruction.opcode == OpCodes.Ldarg_0) 
                {
                    instruction.labels.Add(jumpTable[indexJumpTable]);
                    indexJumpTable++;
                    yield return instruction;
                    continue;
                }

                // else just return the operation, it's a false positive
            }

            // if all previous case were not valid, just return the current instruction
            yield return instruction;
        }
    }
}

[HarmonyPatch]
static class DreadPatch 
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DreadLogic))]
    [HarmonyPatch("ResolveQuirkProbability")]
    static IEnumerable<CodeInstruction> ResolveQuirkProbability(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if(instruction.Is(OpCodes.Ldc_I4_S, 15)) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 20);
            } else {
                yield return instruction;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MissionSummaryCharacterInfo))]
    [HarmonyPatch("AnimateDreadGainRoutine", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> AnimateDreadGainRoutine(IEnumerable<CodeInstruction> instructions)
    {
        bool start_find = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if(!start_find && instruction.opcode == OpCodes.Ldloc_1) {
                start_find = true;
            }

            if(start_find && instruction.opcode == OpCodes.Ldc_I4_5) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_4, null);
            } else {
                yield return instruction;
            }
        }
    }
}

[HarmonyDebug]
[HarmonyPatch]
static class EnergyCellPatch 
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WeaponUpgradeLevelsFactory))]
    [HarmonyPatch("CreateCellUpgrade")]
    static IEnumerable<CodeInstruction> CreateCellUpgrade(IEnumerable<CodeInstruction> instructions)
    {
        int count = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if(count == 0 && instruction.Is(OpCodes.Callvirt, AccessTools.Method(typeof(WeaponStats), "set_MaxHeat"))) 
            {
                yield return instruction;
                count = 1;
            } 
            else if (count is >= 1 and <= 3) 
            {
                count++;
            } 
            else 
            {
                yield return instruction;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WeaponUpgradeLevelsFactory))]
    [HarmonyPatch("CreateCellUpgrades")]
    static IEnumerable<CodeInstruction> CreateCellUpgrades(IEnumerable<CodeInstruction> instructions)
    {
        int level = 1;
        foreach (CodeInstruction instruction in instructions)
        {
            if(instruction.Is(OpCodes.Ldc_I4_S, -2)) 
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, level);
                level++;
            } 
            else 
            {
                yield return instruction;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WeaponOverheatEffectDisplayRoutine))]
    [HarmonyPatch("HandleBackfire", MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> HandleBackfire(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo charfield = typeof(WeaponOverheatEffectDisplayRoutine)
            .GetNestedTypes(AccessTools.all).
            Where(t => t.Name.Contains("HandleBackfire")).
            SelectMany(AccessTools.GetDeclaredFields).
            FirstOrDefault(m => m.Name.Contains("character"));
        
        foreach (CodeInstruction instruction in instructions)
        {
            if(instruction.opcode == OpCodes.Ldc_I4_4) 
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                yield return new CodeInstruction(OpCodes.Ldfld, charfield);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CombatCharacter), "Weapon"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_2, null);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Weapon), "GetUpgradeLevel"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_1, null);
                yield return new CodeInstruction(OpCodes.Add, null);
                yield return new CodeInstruction(OpCodes.Ldc_I4_2, null);
                yield return new CodeInstruction(OpCodes.Div, null);
                yield return new CodeInstruction(OpCodes.Sub, null);
            } 
            else 
            {
                yield return instruction;
            }
        }
    }
}