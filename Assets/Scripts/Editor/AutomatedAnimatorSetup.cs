using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

public class AutomatedAnimatorSetup : Editor
{
    [MenuItem("Tools/1-Click Setup Minion Animator")]
    public static void SetupSelectedAnimator()
    {
        // รับตัว Animator Controller ที่ผู้เล่นกำลังคลิกเลือกไว้ในหน้าต่าง Project
        AnimatorController controller = Selection.activeObject as AnimatorController;

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "กรุณาคลิกเลือกไฟล์ Animator Controller (ไอคอนสี่เหลี่ยมต่อกัน) ในหน้าต่าง Project ก่อนครับ\n\nเช่นคลิกที่ 'Footman' (Controller) ในโฟลเดอร์ Animations", "ตกลง");
            return;
        }

        // 1. เพิ่ม Parameters ให้ตรงกับเป๊ะๆ ตาม Best Practice
        AddParameterIfNotExists(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParameterIfNotExists(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParameterIfNotExists(controller, "TakeDamage", AnimatorControllerParameterType.Trigger);
        AddParameterIfNotExists(controller, "Die", AnimatorControllerParameterType.Trigger);

        // ลบพารามิเตอร์เก่าๆ เช่น Walk ทิ้ง จะได้ไม่งง
        RemoveParameter(controller, "Walk");

        AnimatorStateMachine root = controller.layers[0].stateMachine;

        // 2. สร้างกล่องสถานะ (States) ถ้ายังไม่มี หรือดึงของเก่ามาใช้เผื่อมีการใส่คลิปไว้แล้ว
        AnimatorState idleState = GetOrCreateState(root, "Idle");
        AnimatorState walkState = GetOrCreateState(root, "Walk");
        AnimatorState attackState = GetOrCreateState(root, "Attack");
        AnimatorState hitState = GetOrCreateState(root, "Hit");
        AnimatorState deathState = GetOrCreateState(root, "Death");

        // 3. จัดตำแหน่งกล่องให้สวยงามในหน้าต่าง Animator
        idleState.iKOnFeet = false;
        walkState.iKOnFeet = false;
        
        // ล้างเสัน Transition โบราณทิ้งให้หมดเพื่อเคลียร์ทาง
        idleState.transitions = new AnimatorStateTransition[0];
        walkState.transitions = new AnimatorStateTransition[0];
        root.anyStateTransitions = new AnimatorStateTransition[0];

        // --- 4. ลากเส้นเชื่อมระหว่างด่าน (Transitions) ---
        // Idle -> Walk (เมื่อ Speed > 0.1)
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.1f;

        // Walk -> Idle (เมื่อ Speed < 0.1)
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.1f;

        // --- 5. ลากเส้น AnyState Transitions สำหรับท่าที่เกิดได้ทุกเมื่อ ---
        AnimatorStateTransition anyToAttack = root.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f; // แอนิเมชันจะสลับท่าไวขึ้น ลดความหน่วง

        AnimatorStateTransition anyToHit = root.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "TakeDamage");
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0.1f;

        AnimatorStateTransition anyToDeath = root.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.1f;

        // บังคับให้เริ่มเกมที่ท่า Idle เสมอ
        root.defaultState = idleState;

        // เซฟงาน
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ จัดการ Animator Controller: {controller.name} เสร็จแบบ Best Practice รวดเร็วทันใจ!");
        EditorUtility.DisplayDialog("สำเร็จ!", $"จัดแจง Parameter และเส้นต่างๆ ให้ {controller.name} เรียบร้อยแล้วครับ!\n\nสิ่งที่คุณต้องทำต่อคือ ดับเบิลคลิกไปใส่คลิป (Motion) ให้ครบในแต่ละกล่องเท่านั้น", "ตกลง");
    }

    // ฟังก์ชันเสริม
    private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
        {
            controller.AddParameter(name, type);
        }
    }

    private static void RemoveParameter(AnimatorController controller, string name)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name == name)
            {
                controller.RemoveParameter(i);
                break;
            }
        }
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine root, string name)
    {
        foreach (var state in root.states)
        {
            if (state.state.name == name) return state.state;
        }
        return root.AddState(name);
    }
}
