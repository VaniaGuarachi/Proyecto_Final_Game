using TMPro;
using UnityEngine;

public class UICosechas : MonoBehaviour
{
    public TextMeshProUGUI textoHojaVerde;
    public TextMeshProUGUI textoGotaAzul;
    public TextMeshProUGUI textoEnergiaSolar;
    public TextMeshProUGUI textoNectarMorado;

    public void ActualizarUI(int hojas, int gotas, int energia, int nectar)
    {
        if (textoHojaVerde != null)
        {
            textoHojaVerde.text = "x" + hojas;
        }

        if (textoGotaAzul != null)
        {
            textoGotaAzul.text = "x" + gotas;
        }

        if (textoEnergiaSolar != null)
        {
            textoEnergiaSolar.text = "x" + energia;
        }

        if (textoNectarMorado != null)
        {
            textoNectarMorado.text = "x" + nectar;
        }
    }
}
