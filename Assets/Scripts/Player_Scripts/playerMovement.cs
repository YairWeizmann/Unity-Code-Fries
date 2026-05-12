using UnityEngine;

public class playerMovement : MonoBehaviour
{
    private TileBehaviour m_TileToJump;
    private Vector3 m_playerPosition;



    //Unity Methods
    private void Start()
    {
        getComponents();
    }

    //Helper Methods
    private void getComponents()
    {
        m_playerPosition = transform.position;
    }

    public void teleportTile(TileBehaviour tileToJump)
    {
        float offsetY = 0.7f;

        Vector3 pos = tileToJump.getCenter();
        pos.y = offsetY;

        transform.position = pos;

    }


}
