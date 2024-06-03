using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    //field of view radius variable
    public float Radius;
    //field of view angle variable 
    [Range(0,360)]
    public float Angle;
    //variables for layer masks
    [SerializeField] LayerMask targetMask;
    [SerializeField] LayerMask obsMask;

    //variable for change the color of visual cone
    public Color fovColor;

    //enum object for change plane coordinates
    public enum Plane { XY, YZ, XZ }
    public Plane plane;
    //disc vector and arc vector variables required when plane changes
    [HideInInspector]
    public Vector3 discVector, arcVector;
    //the motion variable is required because the field of view motion will change when the plane coordinates change
    [HideInInspector]
    public float motion;

    //variables for find targets
    [HideInInspector]
    public bool anyObjectDetected;
    public List<Transform>  detectedTargets = new List<Transform>();

    //variables for mesh of visual cone
    [SerializeField] float meshR;
    public MeshFilter visualFilter;
    Mesh visualMesh;

    //variables for edge detection of obstacles
    [HideInInspector]
    public int edgeResolveIterations = 6;
    //a variable for when the first and second ray both hit obstacles to distinguish that they hit different obstacles or not by distance
    [HideInInspector]
    public float edgeDistanceThreshold = 0.5f;

    //a mesh renderer variable for change color of visual cone
    [HideInInspector]
    public MeshRenderer visualConeRenderer;

    void Start()
    {
        //create visualMesh
        visualMesh = new Mesh();
        //give a name to visualMesh
        visualMesh.name = "Cone Mesh";
        //assign to mesh filter (visualFilter) to mesh (visualMesh)
        visualFilter.mesh = visualMesh;
        //coroutine for find targets
        StartCoroutine(FindWithDelay());
        //find mesh of visual cone to change color
        /*
        GameObject visualCone = GameObject.FindGameObjectWithTag("Visual Cone");
        //assign mesh rendered of visual cone to variable
        visualConeRenderer = visualCone.GetComponent<MeshRenderer>();
        //change color with function
        visualConeRenderer.material.color = ChangeColor("blue");
        */
        }
     
    void FixedUpdate()
    {
        DrawField();
        //plane.ToString is required due the control of plane coordinates
        ChangePlane(plane.ToString());
        //if you want to change visual cone color with fovColor variable
        
        GameObject visualCone = GameObject.FindGameObjectWithTag("Visual Cone");
        visualConeRenderer = visualCone.GetComponent<MeshRenderer>();
        visualConeRenderer.material.color = fovColor;
        
    }
       
     
    IEnumerator FindWithDelay()
    {
        //variable for wait to find targets
        WaitForSeconds wait = new WaitForSeconds(0.3f);

        while (true)
        {
            yield return wait;
            FindTargets();
        }
    }

    //method for finding targets
    void FindTargets()
    {
        //clears detectedTargets every time this method called
        detectedTargets.Clear();

        //variable to check if targets are in area
        anyObjectDetected = false;

        //collider array for if targets in visual cone or not
        Collider[] targetInPointOfView = Physics.OverlapSphere(transform.position, Radius, targetMask);

        //loop for to define direction and distance if targets in area 
        for (int i = 0; i <targetInPointOfView.Length; i++)
        {
                //target object for each target in area
                Transform target = targetInPointOfView[i].transform;

                //direction main character to target
                Vector3 direction = (target.position - transform.position).normalized;

                // check angle between target and main character 
                if (Vector3.Angle(transform.forward, direction) < Angle / 2)
                {
                    //distance between target and main character
                    float disToTarget = Vector3.Distance(transform.position, target.position);

                    //to check is there any obstacle between target and character
                    if (!Physics.Raycast(transform.position, direction, disToTarget, obsMask))
                    {
                        anyObjectDetected = true;
                        detectedTargets.Add(target);
                    }
                    else
                        anyObjectDetected = false;
                }
                
        }

    }

    //method for draw field of view
    void DrawField()
    {
        //integer variable for pieces of field of view
        int step = Mathf.RoundToInt(Angle / 2 * meshR);

        //float variable for size of pieces
        float stepSize = Angle / (Mathf.RoundToInt(Angle / 2 * meshR));

        //a list for all the points that raycast hit
        List<Vector3> viewPoints = new List<Vector3> ();

        //variable for to check oldCast hit an obstacle or not
        CastInfo oldCast = new CastInfo();

        //loop for draw lines of field of view
        for (int i = 0; i <= step; i++)
        {
            //angle variable for every piece of field of view
            float angle = motion - Angle / 2 + stepSize * i;
            
           
            //draw pieces of field of view
            //Debug.DrawLine(transform.position, transform.position + DirectionFromAngle(true,angle) * Radius);
            CastInfo newCast = Cast(angle);

            if(i > 0) {
                //a variable to check first and second cast that they hit different obstacles or not 
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldCast.distance - newCast.distance) > edgeDistanceThreshold;
                //if statement for to check is old view cast hit and the new one didn't or opposite
                if (oldCast.hit != newCast.hit || (oldCast.hit && newCast.hit && edgeDistanceThresholdExceeded))
                {
                    //EdgeInfo variable, oldCast as the minimum and newCast as the maximum
                    EdgeInfo edge = FindEdge(oldCast, newCast);
                    //if closest point to the edge on the obstacle not zero, add to the viewPoints array
                    if (edge.closeOn != Vector3.zero)
                    {
                        viewPoints.Add(edge.closeOn);
                    }
                    //if closest point to the edge off the obstacle not zero, add to the viewPoints array
                    if (edge.closeOff != Vector3.zero)
                    {
                        viewPoints.Add(edge.closeOff);
                    }
                }

            }

            //added points which are hit by raycast
            viewPoints.Add(newCast.point);
            oldCast = newCast;


        }
        //vertex count for every piece(triangle) of visual cone
        
        int vertexCount = viewPoints.Count + 1;
        //a Vector3 array to keep vertices
        Vector3[] vertices = new Vector3[vertexCount];
        //an integer array to keep triangles of visual cone
        int[] triangles = new int[(vertexCount) * 3];
        //first vertex is origin(character)
        vertices[0] = Vector3.zero;

        //used "vertexCount -1 " in loop because first vertex is already defined
        for (int i = 0; i < vertexCount - 1; i++)
        {
            //overwrite to first vertex
            //used transform.InverseTransformPoint because view points in global space and convert to local space is needed to make field of view correctly
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            //used "vertexCount - 2" because of range of array
            if (i < vertexCount - 2)
            {
                //when i = 0, the first 3 vertices is used,i = 1 the second 3 vetices is used and so on 
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        //reset everything in mesh
        visualMesh.Clear();
        //assign mesh vertices to vertices array
        visualMesh.vertices = vertices;
        //assign mesh triangles to triangles array
        visualMesh.triangles = triangles;
        //normals are calculated from all vertices (according to unity docs. after modifying vertices )
        visualMesh.RecalculateNormals();
        
    }

    //method for find edge of obstacle
    EdgeInfo FindEdge(CastInfo minCast, CastInfo maxCast)
    {
        //angle variable for min. view cast
        float minAngle = minCast.angle;
        //angle variable for max. view cast
        float maxAngle = maxCast.angle;
        //default variable for min. cast point and max. cast point
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        //for loop for cast out array between in min and max angle (more accurate for edge detection)
        for (int i = 0; i < edgeResolveIterations; i++)
        {
            //float variables for find the angle for each step
            float angle = (minAngle + maxAngle) / 2;
            //cast variable
            CastInfo newCast = Cast(angle);
            //a variable to check first and second cast that they hit different obstacles or not 
            bool edgeDstThresholdExceeded = Mathf.Abs(minCast.distance - newCast.distance) > edgeDistanceThreshold;
            //if newCast hit same thing as minCast, newCast will be assign to minCast
            if (newCast.hit == minCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newCast.point;
            }
            //if newCast hit same thing as maxCast, newCast will be assign to maxCast
            else
            {
                maxAngle = angle;
                maxPoint = newCast.point;
            }
        }
        //return a new EdgeInfo with minPoint,maxPoint
        return new EdgeInfo(minPoint, maxPoint);
    }


    //method for create information of raycast when raycast hit obstacle or not 
    CastInfo Cast(float angle)
    {
        //direction variable
        Vector3 dir = DirectionFromAngle(true, angle);
        //raycast object 
        RaycastHit hit;

        //if raycast hit obstacle, information will create by CastInfo
        if (Physics.Raycast(transform.position, dir, out hit, Radius, obsMask))
        {
            return new CastInfo(true, hit.point, hit.distance, angle);
        }
        else
        {
            return new CastInfo(false, transform.position + dir * Radius, Radius, angle);
        }
    } 
    
    //calculation method for direction
    public Vector3 DirectionFromAngle(bool isGlobal, float angleInDegrees)
    {
        //if angle is not global ("Angle" in FieldOfView.cs), angle will increase every time object moves
        if (!isGlobal)
        {
            angleInDegrees += motion ;
        }

        //if plane is in XZ coordinates calculate direction again
        if (plane.ToString() == "XZ")
        {
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        //if plane is in YZ coordinates calculate direction again
        else if (plane.ToString() == "YZ")
        {
            return new Vector3(0, Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        //if plane is in XZ coordinates calculate direction again
        else if (plane.ToString() == "XY")
        {
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), 0);
        }
        //this is required because method has to return something
        else
            return new Vector3(0,0,0);
    }

    //an information struct for raycast
    public struct CastInfo
    {
        //bool variable for if ray hit something or not
        public bool hit;
        //Vector3 object for end of the ray
        public Vector3 point;
        //float object for distance of the ray
        public float distance;
        //float object for angle of the ray
        public float angle;

        //constructor for struct
        public CastInfo(bool _hit,Vector3 _point, float _distance, float _angle)
        {
            hit = _hit;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }
    
    //method for change vectors and motion directions when plane coordinates are change
    public void ChangePlane(string changedPlane)
    {
        //if plane is in XY coordinates
        if(changedPlane == "XY")
        {
            //discVector variable for draw a disk between the player
            discVector = Vector3.forward;

            //arcVector variable for draw field of view area
            arcVector = Vector3.forward;

            //motion variable indicating which direction the object will move
            motion = transform.eulerAngles.z;
        }

        //if plane is in YZ coordinates
        else if (changedPlane == "YZ")
        {
            //discVector variable for draw a disk between the player
            discVector = Vector3.right;

            //arcVector variable for draw field of view area
            arcVector = Vector3.right;

            //motion variable indicating which direction the object will move
            motion = transform.eulerAngles.x;
        }
         
        //if plane is in XZ coordinates
        else if (changedPlane == "XZ")
        {
            //discVector variable for draw a disk between the player
            discVector = Vector3.up;

            //arcVector variable for draw field of view area
            arcVector = Vector3.down;

            //motion variable indicating which direction the object will move
            motion = transform.eulerAngles.y;
        }
    }

    //method for change the color of any object. newColor is the variable for the color you choose
    public Color ChangeColor(string newColor,float a = 0.30f)
    {
        //if the newColor is red, it changes color of object to red with changing "rgb" properties of object's color
        if(newColor == "red")
        {
            return new Color(1, 0, 0, a);
        }

        //if the newColor is black, it changes color of object to black with changing "rgb" properties of object's color
        else if (newColor == "black")
        {
            return new Color(0, 0, 0, a);
        }

        //if the newColor is blue, it changes color of object to blue with changing "rgb" properties of object's color
        else if (newColor == "blue")
        {
            return new Color(0,0,1,a);
        }

        //if the newColor is green, it changes color of object to green with changing "rgb" properties of object's color
        else if (newColor == "green")
        {
            return new Color(0, 1, 0, a);
        }
         
        //if the newColor is cyan, it changes color of object to cyan with changing "rgb" properties of object's color
        else if (newColor == "cyan")
        {
            return new Color(0, 1, 1, a);
        }
         
        //if the newColor is grey, it changes color of object to grey with changing "rgb" properties of object's color
        else if (newColor == "grey")
        {
            return new Color(0.5f, 0.5f, 0.5f, a);
        }

        //if the newColor is magenta, it changes color of object to magenta with changing "rgb" properties of object's color
        else if (newColor == "magenta")
        {
            return new Color(1, 0, 1, a);
        }

        //if the newColor is white, it changes color of object to white with changing "rgb" properties of object's color
        else if (newColor == "white")
        {
            return new Color(1, 1, 1, a);
        }

        //if the newColor is yellow, it changes color of object to yellow with changing "rgb" properties of object's color
        else if (newColor == "yellow")
        {
            return new Color(1, 0.92f, 0.016f, a);
        }

        //if newColor is not one of the these colors(red,black,blue,green,cyan,grey,magenta,white,yellow) rgb properties will be 0
        else
        {
            return new Color(0, 0, 0, 0);
        }
    }

    //struct for edge of field of view on obstacles
    public struct EdgeInfo
    {
        //closest point to the edge on the obstacle
        public Vector3 closeOn;
        //closest point to the edge off the obstacle
        public Vector3 closeOff;

        //constructor for EdgeInfo
        public EdgeInfo(Vector3 _closeOn, Vector3 _closeOff)
        {
            closeOn = _closeOn;
            closeOff = _closeOff;
        }
        
    }

    
                          
}   
  
 

   