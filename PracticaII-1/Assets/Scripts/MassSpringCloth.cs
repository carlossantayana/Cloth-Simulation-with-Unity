using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassSpringCloth : MonoBehaviour
{
    public bool paused; //Booleano que se encarga de pausar y reanudar la animaci�n.

    public enum Integration
    {
        ExplicitEulerIntegration = 0,
        SymplecticEulerIntegration = 1,
    }

    public Integration integrationMethod; //Variable con la que escoger el m�todo de integraci�n a utilizar.

    Mesh mesh; //Mallado triangular de la tela.
    Vector3[] vertices; //Array que almacena en cada posici�n la posici�n 3D de cada v�rtice del mallado.
    List<Node> nodes; //Lista de objetos de la clase nodo que almacenan las propiedas f�sicas de los v�rtices del mallado para el c�lculo de la animaci�n.
    List<Spring> springs = new List<Spring>();
    int[] triangles; //Lista que almacena 3 enteros por tri�ngulo de la malla.
    List<Edge> edges = new List<Edge>(); //Lista que almacena todas las aristas de la malla.

    public float clothMass = 0.5f; //Masa total de la tela, repartida equitativamente entre cada uno de los nodos de masa que la componen.
    public float tractionSpringStiffness = 5.0f; //Constante de rigidez de los muelles de tracci�n. La tela no es muy el�stica.
    public float flexionSpringStiffness = 1.0f; //Constante de rigidez de los muelles de flexi�n. Sin embargo, s� se dobla f�cilmente.

    public float dAbsolute = 0.1f; //Constante de amortiguamiento (damping) absoluto sobre la velocidad de los nodos.

    public Vector3 g = new Vector3(0.0f, 9.8f, 0.0f); //Constante gravitacional.

    public float h = 0.01f; //Tama�o del paso de integraci�n de las f�sicas de la animaci�n.

    // Start is called before the first frame update
    void Start()
    {
        paused = true;
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        nodes = new List<Node>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            nodes.Add(new Node(i, transform.TransformPoint(vertices[i]), clothMass/vertices.Length));
        }

        triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Edge A = new Edge(triangles[i], triangles[i+1], triangles[i+2]);
            Edge B = new Edge(triangles[i], triangles[i + 2], triangles[i + 1]);
            Edge C = new Edge(triangles[i + 1], triangles[i + 2], triangles[i]);

            edges.Add(A); edges.Add(B); edges.Add(C);
        }

        edges.Sort();

        Edge previousEdge = null;

        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].Equals(previousEdge))
            {
                springs.Add(new Spring(flexionSpringStiffness, nodes[edges[i].vertexOther], nodes[previousEdge.vertexOther]));
            }
            else
            {
                springs.Add(new Spring(tractionSpringStiffness, nodes[edges[i].vertexA], nodes[edges[i].vertexB]));
            }

            previousEdge = edges[i];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            paused = !paused;
        }
    }

    //La integraci�n de las f�sicas se realiza en la actualizaci�n de paso fijo, pues as� se evita la acumulaci�n de error.
    private void FixedUpdate()
    {
        if (paused)
        {
            return;
        }

        switch (integrationMethod)
        {
            case Integration.ExplicitEulerIntegration:
                IntegrateExplicitEuler();
                break;
            case Integration.SymplecticEulerIntegration:
                IntegrateSymplecticEuler();
                break;
            default:
                Debug.Log("Error: m�todo de integraci�n no encontrado");
                break;
        }

        foreach(Spring spring in springs)
        {
            spring.UpdateSpring();
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = transform.InverseTransformPoint(nodes[i].pos);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    void IntegrateExplicitEuler()
    {

    }

    void IntegrateSymplecticEuler()
    {
        foreach (Node node in nodes)
        {
            node.force = -node.mass * g;
        }

        foreach (Spring spring in springs)
        {
            spring.nodeA.force += -spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            spring.nodeB.force += spring.k * (spring.lenght - spring.lenght0) * spring.dir;
        }

        foreach (Node node in nodes)
        {
            if (node.vertexID != 0 && node.vertexID != 110)
            {
                node.vel += h * node.force / node.mass;
                node.pos += h * node.vel;
            }
        }
    }
}
