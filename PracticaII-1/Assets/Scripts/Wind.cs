using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind : MonoBehaviour
{
    public float maxWindForce = 40;

    private Vector3 _windDirection;
    [HideInInspector]
    public Vector3 WindDirection {  get { return _windDirection; } set {  _windDirection = value; } }

    private float _windIntensity = 0f;
    [HideInInspector]
    public float WindIntensity { get { return _windIntensity; } set { _windIntensity = value; } }

    private float _rotationSpeed = 60f;

    private float _incrementationSpeed = 0.5f;

    private const int CYLINDER_DEFAULT_SIZE = 2;
    private int _cylinderMaxSize = 8;

    private float _translationSpeed;

    GameObject intensity_gizmo;
    GameObject direction_gizmo;

    // Start is called before the first frame update
    void Start()
    {
        intensity_gizmo = transform.Find("Wind_Intensity_Gizmo").gameObject;
        direction_gizmo = transform.Find("Wind_Direction_Gizmo").gameObject;

        _translationSpeed = _cylinderMaxSize * _incrementationSpeed;
        _windDirection = transform.forward;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(new Vector3(0f, -_rotationSpeed*Time.deltaTime, 0f));
            _windDirection = transform.forward;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(new Vector3(0f, _rotationSpeed * Time.deltaTime, 0f));
            _windDirection = transform.forward;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (_windIntensity < 1)
            {
                _windIntensity += _incrementationSpeed * Time.deltaTime;
                intensity_gizmo.transform.Translate(0, Time.deltaTime * _translationSpeed / 2, 0, Space.Self);
                direction_gizmo.transform.Translate(0, Time.deltaTime * _translationSpeed, 0, Space.Self);
            }
            else
            {
                _windIntensity = 1;
            }

            intensity_gizmo.transform.localScale = new Vector3(intensity_gizmo.transform.localScale.x, _windIntensity * _cylinderMaxSize/CYLINDER_DEFAULT_SIZE, intensity_gizmo.transform.localScale.z);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            if(_windIntensity > 0)
            {
                _windIntensity -= _incrementationSpeed * Time.deltaTime;
                intensity_gizmo.transform.Translate(0, -Time.deltaTime * _translationSpeed / 2, 0, Space.Self);
                direction_gizmo.transform.Translate(0, -Time.deltaTime * _translationSpeed, 0, Space.Self);
            }
            else
            {
                _windIntensity = 0;
            }

            intensity_gizmo.transform.localScale = new Vector3(intensity_gizmo.transform.localScale.x, _windIntensity * _cylinderMaxSize / CYLINDER_DEFAULT_SIZE, intensity_gizmo.transform.localScale.z);
        }
    }
}
