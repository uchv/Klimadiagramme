using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class KDInput : MonoBehaviour
{
    public enum Datentyp
    {
        TEMPERATUR,
        NIEDERSCHLAG
    }
    public enum Monat
    {
        JANUAR,
        FEBRUAR,
        MÄRZ,
        APRIL, 
        MAI,
        JUNI,
        JULI,
        AUGUST,
        SEPTEMBER,
        OKTOBER,
        NOVEMBER,
        DEZEMBER
    }
    public Monat monthIndex;
    public Datentyp typ;

    public float GetValue()
    {
        string input = GetComponent<TMPro.TMP_InputField>().text;
        if(input.Length == 0)
        {
            return -999.0f;
        }
        return float.Parse(input); // , CultureInfo.InvariantCulture.NumberFormat);
    }
}
