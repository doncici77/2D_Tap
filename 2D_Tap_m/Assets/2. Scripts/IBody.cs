using UnityEngine;

public interface IBody
{
    void PushOpponent(float powerMultiplier);
    void PlaySuccessSound();
    void PlayFailSound();
    void PlayAttackAnim();
}
