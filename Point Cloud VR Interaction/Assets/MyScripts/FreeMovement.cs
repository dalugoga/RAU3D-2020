using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeMovement : MonoBehaviour
{

    public GameObject radial_menu;
    public VRTK.VRTK_ControllerEvents left_controller_events;
    public VRTK.VRTK_ControllerEvents right_controller_events;
    Transform play_area;
    bool is_left_grip_pressed = false;
    bool is_right_grip_pressed = false;
    Vector3 controller_pos_start;

    // Start is called before the first frame update
    void Start()
    {
        play_area = VRTK.VRTK_DeviceFinder.PlayAreaTransform();

        left_controller_events.GripPressed += new VRTK.ControllerInteractionEventHandler(DoGripPressedLeft);
        left_controller_events.GripReleased += new VRTK.ControllerInteractionEventHandler(DoGripReleasedLeft);
        right_controller_events.GripPressed += new VRTK.ControllerInteractionEventHandler(DoGripPressedRight);
        right_controller_events.GripReleased += new VRTK.ControllerInteractionEventHandler(DoGripReleasedRight);
    }

    // Update is called once per frame
    void Update()
    {
        if(play_area == null)
            play_area = VRTK.VRTK_DeviceFinder.PlayAreaTransform();

        if (is_left_grip_pressed && !is_right_grip_pressed)
        {
            radial_menu.SetActive(false);
            Vector3 controller_pos = VRTK.VRTK_DeviceFinder.GetControllerLeftHand().transform.position;
            Vector3 delta_pos = controller_pos - controller_pos_start;
            Vector3 play_area_position = play_area.position;
            if (Mathf.Abs(delta_pos.y) > 0.05)
            {
                play_area_position.y = play_area.position.y + 0.01f * Mathf.Sign(delta_pos.y);
                play_area.position = play_area_position;
                controller_pos_start.y = controller_pos_start.y + 0.01f * Mathf.Sign(delta_pos.y);
            }

            if (Mathf.Abs(delta_pos.y) > 0.1)
            {
                play_area_position.y = play_area.position.y + 0.05f * Mathf.Sign(delta_pos.y);
                play_area.position = play_area_position;
                controller_pos_start.y = controller_pos_start.y + 0.05f * Mathf.Sign(delta_pos.y);
            }
        }
        else
            radial_menu.SetActive(true);


    }

    public void DoGripPressedLeft(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        is_left_grip_pressed = true;
        controller_pos_start = e.controllerReference.actual.transform.position;
    }

    public void DoGripReleasedLeft(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        is_left_grip_pressed = false;
    }

    public void DoGripPressedRight(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        is_right_grip_pressed = true;
    }

    public void DoGripReleasedRight(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        is_right_grip_pressed = false;
    }
}
