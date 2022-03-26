using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

public class DistributingInputModule : BaseInputModule
{

    private List<BaseInputModule> GetInputModules()
    {
        EventSystem current = EventSystem.current;
        FieldInfo m_SystemInputModules = current.GetType().GetField("m_SystemInputModules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return m_SystemInputModules.GetValue(current) as List<BaseInputModule>;
    }

    private void SetInputModules(List<BaseInputModule> inputModules)
    {
        EventSystem current = EventSystem.current;
        FieldInfo m_SystemInputModules = current.GetType().GetField("m_SystemInputModules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        m_SystemInputModules.SetValue(current, inputModules);
    }

    public override void UpdateModule()
    {
        MethodInfo changeEventModuleMethod =
            EventSystem.current.GetType().GetMethod("ChangeEventModule",
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(BaseInputModule) }, null);
        changeEventModuleMethod.Invoke(EventSystem.current, new object[] { this });
        EventSystem.current.UpdateModules();
        List<BaseInputModule> activeInputModules = GetInputModules();
        activeInputModules.Remove(this);
        activeInputModules.Insert(0, this);
        SetInputModules(activeInputModules);
    }

    public override void Process()
    {
        List<BaseInputModule> activeInputModules = GetInputModules();
        foreach (BaseInputModule module in activeInputModules)
        {
            if (module == this)
                continue;

            Debug.Log("Processing " + module.GetType().Name + " module");
            module.Process();
        }
    }
}