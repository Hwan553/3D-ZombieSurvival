using UnityEngine;

public class LaserController : MonoBehaviour
{
    public LineRenderer laserLine;
    public Transform firePoint;
    public LayerMask hitLayers;

    private bool isLaserActive = true;

    public float maxLaserDistance = 0.5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isLaserActive = !isLaserActive;
            laserLine.enabled = isLaserActive;
        }

        if (isLaserActive)
        {
            UpdateLaser();
        }
    }

    void UpdateLaser()
    {
        laserLine.SetPosition(0, firePoint.position);

        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxLaserDistance, hitLayers))
        {
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            laserLine.SetPosition(1, firePoint.position + firePoint.forward * maxLaserDistance);
        }
    }
}
