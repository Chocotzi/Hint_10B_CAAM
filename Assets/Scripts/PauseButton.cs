using UnityEngine;
using UnityEngine.UI;

// Boton de Pausa en la UI: pausa/reanuda el juego y reproduce el SFX de seleccion.
public class PauseButton : MonoBehaviour
{
    public Text label;

    public void TogglePause()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxUISelect, false);

        if (Game.obj == null) return;

        Game.obj.gamePaused = !Game.obj.gamePaused;
        Time.timeScale = Game.obj.gamePaused ? 0f : 1f;

        if (label != null)
            label.text = Game.obj.gamePaused ? "REANUDAR" : "PAUSA";
    }
}
