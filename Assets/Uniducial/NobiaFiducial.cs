using System;
using System.Collections.Generic;
using UnityEngine;


public class NobiaFiducial : MonoBehaviour
{
	public int MarkerID = 0;
	
	public enum RotationAxis { Forward, Back, Up, Down, Left, Right };
	
	//translation
	public bool IsPositionMapped = true;
	public bool InvertX = false;
	public bool InvertY = false;
	
	//rotation
	public bool IsRotationMapped = true;
	public bool AutoHideGO = false;
	private bool m_ControlsGUIElement = false;

	//grid
	public bool fitGrid = false;
	public float gridSize = 1;
	public int rotationGridSize = 10;
	
	public float CameraOffset = 10;
	public RotationAxis RotateAround = RotationAxis.Up;
	private UniducialLibrary.TuioManager m_TuioManager;
	private Camera m_MainCamera;
	
	//members
	private Vector2 m_ScreenPosition;
	private Vector3 m_WorldPosition;
	private Vector2 m_Direction;
	private float m_Angle;
	private float m_AngleDegrees;
	private float m_Speed;
	private float m_Acceleration;
	private float m_RotationSpeed;
	private float m_RotationAcceleration;
	private bool m_IsVisible;
	
	public float RotationMultiplier = 1;
	
	void Awake()
	{
		this.m_TuioManager = UniducialLibrary.TuioManager.Instance;
		this.m_TuioManager.Connect();

		this.m_ScreenPosition = Vector2.zero;
		this.m_WorldPosition = Vector3.zero;
		this.m_Direction = Vector2.zero;
		this.m_Angle = 0f;
		this.m_AngleDegrees = 0;
		this.m_Speed = 0f;
		this.m_Acceleration = 0f;
		this.m_RotationSpeed = 0f;
		this.m_RotationAcceleration = 0f;
		this.m_IsVisible = true;
	}
	
	void Start()
	{
		//get reference to main camera
		this.m_MainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		
		//check if the main camera exists
		if (this.m_MainCamera == null)
		{
			Debug.LogError("There is no main camera defined in your scene.");
		}
	}
	
	void Update()
	{
		if (this.m_TuioManager.IsConnected
		    && this.m_TuioManager.IsMarkerAlive(this.MarkerID))
		{
			TUIO.TuioObject marker = this.m_TuioManager.GetMarker(this.MarkerID);
			
			//update parameters
			this.m_ScreenPosition.x = marker.getX();
			this.m_ScreenPosition.y = marker.getY();
			this.m_Angle = marker.getAngle() * RotationMultiplier;
			this.m_AngleDegrees = marker.getAngleDegrees() * RotationMultiplier;
			this.m_Speed = marker.getMotionSpeed();
			this.m_Acceleration = marker.getMotionAccel();
			this.m_RotationSpeed = marker.getRotationSpeed() * RotationMultiplier;
			this.m_RotationAcceleration = marker.getRotationAccel();
			this.m_Direction.x = marker.getXSpeed();
			this.m_Direction.y = marker.getYSpeed();
			this.m_IsVisible = true;

			if( this.fitGrid ) {
				//fit position into the grid
				int colX = (int) (marker.getX() / this.gridSize);
				this.m_ScreenPosition.x = (float) (colX * this.gridSize);
				int colY = (int) (marker.getY() / this.gridSize);
				this.m_ScreenPosition.y = (float) (colY * this.gridSize);

				//fit rotation angle into a grid
				int step = (int) (marker.getAngleDegrees() / this.rotationGridSize);
				this.m_AngleDegrees = (float) (step * this.rotationGridSize);
			}
			
			//set game object to visible, if it was hidden before
			ShowGameObject();
			
			//update transform component
			UpdateTransform();
		}
		else
		{
			//automatically hide game object when marker is not visible
			if (this.AutoHideGO)
			{
				HideGameObject();
			}
			
			this.m_IsVisible = false;
		}
	}
	
	
	void OnApplicationQuit()
	{
		if (this.m_TuioManager.IsConnected)
		{
			this.m_TuioManager.Disconnect();
		}
	}
	
	private void UpdateTransform()
	{
		//position mapping
		if (this.IsPositionMapped)
		{
			//calculate world position with respect to camera view direction
			float xPos = this.m_ScreenPosition.x;
			float yPos = this.m_ScreenPosition.y;
			if (this.InvertX) xPos = 1 - xPos;
			if (this.InvertY) yPos = 1 - yPos;
			
			if (this.m_ControlsGUIElement)
			{
				transform.position = new Vector3(xPos, 1 - yPos, 0);
			}
			else
			{
				Vector3 position = new Vector3(xPos * Screen.width,
				                               (1 - yPos) * Screen.height, this.CameraOffset);
				this.m_WorldPosition = this.m_MainCamera.ScreenToWorldPoint(position);
				//worldPosition += cameraOffset * mainCamera.transform.forward;
				transform.position = this.m_WorldPosition;
			}
		}
		
		//rotation mapping
		if (this.IsRotationMapped)
		{
			Quaternion rotation = Quaternion.identity;
			
			switch (this.RotateAround)
			{
			case RotationAxis.Forward:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.forward);
				break;
			case RotationAxis.Back:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.back);
				break;
			case RotationAxis.Up:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.up);
				break;
			case RotationAxis.Down:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.down);
				break;
			case RotationAxis.Left:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.left);
				break;
			case RotationAxis.Right:
				rotation = Quaternion.AngleAxis(this.m_AngleDegrees, Vector3.right);
				break;
			}
			transform.localRotation = rotation;
		}
	}
	
	private void ShowGameObject()
	{
		if (this.m_ControlsGUIElement)
		{
			//show GUI components
			if (gameObject.GetComponent<GUIText>() != null && !gameObject.GetComponent<GUIText>().enabled)
			{
				gameObject.GetComponent<GUIText>().enabled = true;
			}
			if (gameObject.GetComponent<GUITexture>() != null && !gameObject.GetComponent<GUITexture>().enabled)
			{
				gameObject.GetComponent<GUITexture>().enabled = true;
			}
		}
		else
		{
			if (gameObject.GetComponent<Renderer>() != null && !gameObject.GetComponent<Renderer>().enabled)
			{
				gameObject.GetComponent<Renderer>().enabled = true;
			}
		}
	}
	
	private void HideGameObject()
	{
		if (this.m_ControlsGUIElement)
		{
			//hide GUI components
			if (gameObject.GetComponent<GUIText>() != null && gameObject.GetComponent<GUIText>().enabled)
			{
				gameObject.GetComponent<GUIText>().enabled = false;
			}
			if (gameObject.GetComponent<GUITexture>() != null && gameObject.GetComponent<GUITexture>().enabled)
			{
				gameObject.GetComponent<GUITexture>().enabled = false;
			}
		}
		else
		{
			//set 3d game object to visible, if it was hidden before
			if (gameObject.GetComponent<Renderer>() != null && gameObject.GetComponent<Renderer>().enabled)
			{
				gameObject.GetComponent<Renderer>().enabled = false;
			}
		}
	}
	
	#region Getter
	
	public bool isAttachedToGUIComponent()
	{
		return (gameObject.GetComponent<GUIText>() != null || gameObject.GetComponent<GUITexture>() != null);
	}
	public Vector2 ScreenPosition
	{
		get { return this.m_ScreenPosition; }
	}
	public Vector3 WorldPosition
	{
		get { return this.m_WorldPosition; }
	}
	public Vector2 MovementDirection
	{
		get { return this.m_Direction; }
	}
	public float Angle
	{
		get { return this.m_Angle; }
	}
	public float AngleDegrees
	{
		get { return this.m_AngleDegrees; }
	}
	public float Speed
	{
		get { return this.m_Speed; }
	}
	public float Acceleration
	{
		get { return this.m_Acceleration; }
	}
	public float RotationSpeed
	{
		get { return this.m_RotationSpeed; }
	}
	public float RotationAcceleration
	{
		get { return this.m_RotationAcceleration; }
	}
	public bool IsVisible
	{
		get { return this.m_IsVisible; }
	}
	#endregion
}

