using UnityEngine;

public class RoleController : MonoBehaviour
{

    private CharacterRole currentRole;

    public CharacterRole CurrentRole => currentRole;

    public bool IsChaser =>
        currentRole == CharacterRole.Chaser;

    public bool IsEvader =>
        currentRole == CharacterRole.Evader;

    public void SetRole(CharacterRole newRole)
    {
        currentRole = newRole;
    }
}