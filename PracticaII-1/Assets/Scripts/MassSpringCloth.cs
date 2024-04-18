using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassSpringCloth : MonoBehaviour
{
    public bool paused; //Booleano que se encarga de pausar y reanudar la animación.

    public enum Integration
    {
        ExplicitEulerIntegration = 0,
        SymplecticEulerIntegration = 1,
    }

    public Integration integrationMethod; //Variable con la que escoger el método de integración a utilizar.

    Mesh mesh; //Mallado triangular de la tela.
    Vector3[] vertices; //Array que almacena en cada posición la posición 3D de cada vértice del mallado.
    List<Node> nodes; //Lista de objetos de la clase nodo que almacenan las propiedas físicas de los vértices del mallado para el cálculo de la animación.
    List<Spring> springs = new List<Spring>();
    int[] triangles; //Lista que almacena 3 enteros por triángulo de la malla.
    List<Edge> edges = new List<Edge>(); //Lista que almacena todas las aristas de la malla.

    public float clothMass = 0.5f; //Masa total de la tela, repartida equitativamente entre cada uno de los nodos de masa que la componen.
    public float tractionSpringStiffness = 5.0f; //Constante de rigidez de los muelles de tracción. La tela no es muy elástica.
    public float flexionSpringStiffness = 1.0f; //Constante de rigidez de los muelles de flexión. Sin embargo, sí se dobla fácilmente.

    public float dAbsolute = 0.1f; //Constante de amortiguamiento (damping) absoluto sobre la velocidad de los nodos.

    public Vector3 g = new Vector3(0.0f, 9.8f, 0.0f); //Constante gravitacional.

    public float h = 0.01f; //Tamaño del paso de integración de las físicas de la animación.

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

    //La integración de las físicas se realiza en la actualización de paso fijo, pues así se evita la acumulación de error.
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
                Debug.Log("Error: método de integración no encontrado");
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
