using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Jobs;
using UnityEngine;

namespace PUL
{
    /// <summary>
    /// An attempt to implement the Sugiyama method for hierarchical graphs. Positions nodes on 2D plane in 3D space.
    /// Implementation adapted from a Java Universal Network/Graph Framework (JUNG) implementation
    /// found at https://sourceforge.net/p/jung/patches/10/ by C. Schanck (chris at schanck dot net)
    /// 
    ///
    /// Usage:
    /// -Attach this component to a GameObject in a scene.
    /// -Add nodes by calling <see cref="AddNodeToGraph"/> 
    /// -Add edges by calling <see cref="AddEdgeToGraph"/> and passing two nodes that have been
    ///  previously added to the graph
    /// -Run and Stop the graph using <see cref="StartGraph"/> and <see cref="StopGraph"/>. 
    /// </summary>
    public class SugiyamaGraph : BasicGraph
    {
        // Graph nodes
        public List<NodeInfo> nodes = new();

        private static Orientation DEFAULT_ORIENTATION = Orientation.TOP;

        private bool executed = false;

        // represents the size of the grid in horizontal grid elements
        private int gridAreaSize = Int32.MinValue;

        private HashSet<NodeInfo> traversalSet = new HashSet<NodeInfo>();

        private Dictionary<NodeInfo, CellWrapper> vertToWrapper = new Dictionary<NodeInfo, CellWrapper>();

        private Orientation orientation;


        /// <summary>
        /// Adds a <see cref="Node"/> component to the component gameobject that is passed. When the graph is run,
        /// this behaviour will move the gameobject as it responds to forces in the graph.
        /// </summary>
        /// <param name="gameObject">The gameobject that will have a node attached.</param>
        [PublicAPI]
        public override NodeInfo AddNodeToGraph(GameObject gameObject)
        {
            NodeInfo nodeInfo = base.AddNodeToGraph(gameObject);
            nodes.Add(nodeInfo);
            return nodeInfo;
        }

        [PublicAPI]
        public override EdgeInfo AddEdgeToGraph(NodeInfo sourceNode, NodeInfo targetNode)
        {
            EdgeInfo edgeInfo = base.AddEdgeToGraph(sourceNode, targetNode);
            return edgeInfo;
        }

        [PublicAPI]
        public override void StartGraph(GameObject graphHandle)
        {
            StartCoroutine(StartGraphCoroutine(graphHandle));
        }

        IEnumerator StartGraphCoroutine(GameObject graphHandle)
        {
            this.orientation = DEFAULT_ORIENTATION;

            initialize(graphHandle);

            yield return new WaitForEndOfFrame(); // put this somewhere smarter!
        }

        // Determine the placement of the nodes on a grid, then position the nodes in space
        public void initialize(GameObject graphHandle)
        {
            if (!executed)
            {
                // Determine the level placement for each node
                List<List<CellWrapper>> graphLevels = runSugiyama();

                // Keep track of min and max extents in X and Y dimensions so we can resize the 
                // graph's overall collider after we place all the nodes.
                BoxCollider collider = graphHandle.GetComponent<BoxCollider>();
                float minX = -(collider.size.x / 2f);
                float maxX = collider.size.x / 2f;
                float minY = -(collider.size.y / 2f);
                float maxY = collider.size.y / 2f;
                // Debug.Log($"BEFORE MinX: {minX} MaxX: {maxX} MinY: {minY} MaxY: {maxY}");

                // Place the nodes in space.
                // First walk through each level
                foreach (List<CellWrapper> level in graphLevels)
                {
                    // Then walk through each node in that level to determine grid position
                    foreach (CellWrapper wrapper in level)
                    {
                        NodeInfo vertex = wrapper.getVertexView();

                        // Place the node based on its size and grid position
                        Vector3 size = vertex.nodeGameObject.GetComponent<Collider>().bounds.size;

                        // Where to draw the graph in relation to the graph handle object's center
                        float xOffset = 0.25f;
                        float yOffset = -0.25f;

                        // Place the nodes in either top-down or left-right orientation
                        if (orientation == Orientation.TOP)
                        {
                            float x = ((size.x * 1.4f) * wrapper.gridPosition) + xOffset;
                            float y = -(size.y * 1.4f * wrapper.level) + yOffset;
                            vertex.transform.localPosition = new Vector3(x, y, 0.0f);
                            if ((x - (size.x / 2f)) < minX) minX = x - (size.x / 2f);
                            if ((x + (size.x / 2f)) > maxX) maxX = x + (size.x / 2f);
                            if ((y - (size.y / 2f)) < minY) minY = y - (size.y / 2f);
                            if ((y + (size.y / 2f)) > maxY) maxY = y + (size.y / 2f);
                        }
                        else
                        {
                            float y = -(size.y * 1.4f * wrapper.gridPosition) + yOffset;
                            float x = ((size.x * 1.4f) * wrapper.level) + xOffset;
                            vertex.transform.localPosition = new Vector3(x, y, 0.0f);
                            if ((x - (size.x / 2f)) < minX) minX = x - (size.x / 2f);
                            if ((x + (size.x / 2f)) > maxX) maxX = x + (size.x / 2f);
                            if ((y - (size.y / 2f)) < minY) minY = y - (size.y / 2f);
                            if ((y + (size.y / 2f)) > maxY) maxY = y + (size.y / 2f);
                        }
                    }
                }
                // Debug.Log($" AFTER MinX: {minX} MaxX: {maxX} MinY: {minY} MaxY: {maxY}");

                // Update size and local position of the overall graph collider 
                float newSizeX = maxX - minX;
                float newSizeY = maxY - minY;
                collider.size = new Vector3(newSizeX, newSizeY, collider.size.z);
                collider.center = new Vector3(minX + (newSizeX / 2f), minY + (newSizeY / 2f), collider.center.z);
                Debug.Log($"NEW GRAPH COLLIDER CENTER {collider.center}");
                Debug.Log($"NEW GRAPH COLLIDER SIZE {collider.size}");
            }
        }

        private List<List<CellWrapper>> runSugiyama()
        {
            executed = true;

            HashSet<NodeInfo> vertexSet = new HashSet<NodeInfo>(nodes);

            // search all roots
            List<NodeInfo> roots = searchRoots(vertexSet);

            // create levels -> its a List of Lists
            List<List<CellWrapper>> levels = fillLevels(roots, vertexSet);

            // solves the edge crosses
            solveEdgeCrosses(levels);

            // move all nodes into the barycenter
            moveToBarycenter(levels, vertexSet);

            // you could probably nuke the maps at this point, but i'm not certain, and I don't care.
            return levels;
        }

        /** Searches all Roots for the current Graph
        * First the method marks any Node as not visited.
        * Than calls searchRoots(MyGraphCell) for each
        * not visited Cell.
        * The Roots are stored in the List named roots
        *
        * @return returns a List with the roots
        * @see #searchRoots(JGraph, CellView[])
        */
        private List<NodeInfo> searchRoots(HashSet<NodeInfo> vertexSet)
        {
            List<NodeInfo> roots = new List<NodeInfo>();
            // first: mark all as not visited
            // it is assumed that vertex are not visited
            foreach (NodeInfo vert in vertexSet)
            {
                if (!traversalSet.Contains(vert))
                {
                    traversalSet.Add(vert);

                    int in_degree = vert.sourceEdgeInfos.Count; 
                    if (in_degree == 0)
                    {
                        roots.Add(vert);
                    }
                }
            }
            return roots;
        }

        /** Method fills the levels and stores them in the member levels.
        * Each level was represended by a List with Cell Wrapper objects.
        * These Lists are the elements in the <code>levels</code> List.
        */
        private List<List<CellWrapper>> fillLevels(List<NodeInfo> roots, HashSet<NodeInfo> vertexSet)
        {
            List<List<CellWrapper>> levels = new List<List<CellWrapper>>();

            // clear the visit
            traversalSet.Clear();

            foreach (NodeInfo r in roots)
            {
                fillLevels(levels, 0, r); // 0 indicates level 0
            } // i.e root level
            return levels;
        }

        /** Fills the List for the specified level with a wrapper
        * for the MyGraphCell. After that the method called for
        * each neighbor graph cell.
        * @param level The level for the graphCell
        * @param graphCell The Graph Cell
        */
        private void fillLevels(List<List<CellWrapper>> levels, int level, NodeInfo rootNode)
        { // this is a recursive function
            // precondition control
            if (rootNode == null)
                return;

            // be sure that a List container exists for the current level
            if (levels.Count == level)
                levels.Insert(level, new List<CellWrapper>());

            // if the cell already visited return
            if (traversalSet.Contains(rootNode))
            {
                return;
            }

            // mark as visited for cycle tests
            traversalSet.Add(rootNode);

            // put the current node into the current level
            // get the Level List
            List<CellWrapper> vecForTheCurrentLevel = (List<CellWrapper>)levels[level];

            // Create a wrapper for the node
            int numberForTheEntry = vecForTheCurrentLevel.Count;

            CellWrapper wrapper = new CellWrapper(level, numberForTheEntry, rootNode);

            // put the Wrapper in the LevelList
            vecForTheCurrentLevel.Add(wrapper);

            // concat the wrapper to the cell for an easy access
            //vertexView.getAttributes().put(SUGIYAMA_CELL_WRAPPER, wrapper);
            vertToWrapper[rootNode] = wrapper;

            foreach(EdgeInfo edge in rootNode.targetEdgeInfos)
            {
                NodeInfo targetVertex = edge.targetNodeInfo;
                fillLevels(levels, level + 1, targetVertex);
            }

            if (vecForTheCurrentLevel.Count > gridAreaSize)
            {
                gridAreaSize = vecForTheCurrentLevel.Count;
            }
        }

        private void solveEdgeCrosses(List<List<CellWrapper>> levels)
        {
            int movementsCurrentLoop = -1;

            while (movementsCurrentLoop != 0)
            {
                // reset the movements per loop count
                movementsCurrentLoop = 0;

                // top down
                for (int i = 0; i < levels.Count - 1; i++)
                {
                    movementsCurrentLoop += solveEdgeCrosses(true, levels, i);
                }

                // bottom up
                for (int i = levels.Count - 1; i >= 1; i--)
                {
                    movementsCurrentLoop += solveEdgeCrosses(false, levels, i);
                }
            }
        }

        /**
        * @return movements
        */
        private int solveEdgeCrosses(bool down, List<List<CellWrapper>> levels, int levelIndex)
        {
            // Get the current level
            List<CellWrapper> currentLevel = levels[levelIndex];
            int movements = 0;

            // restore the old sort
            CellWrapper[] levelSortBefore = currentLevel.ToArray();

            // new sort
            currentLevel.Sort();

            // test for movements
            for (int j = 0; j < levelSortBefore.Length; j++)
            {
                if ((levelSortBefore[j]).getEdgeCrossesIndicator() != (currentLevel[j]).getEdgeCrossesIndicator())
                {
                    movements++;
                }
            }
            // Collections Sort sorts the highest value to the first value
            for (int j = currentLevel.Count - 1; j >= 0; j--)
            {
                CellWrapper sourceWrapper = currentLevel[j];

                NodeInfo sourceView = sourceWrapper.getVertexView();

                List<EdgeInfo> edgeList = getNeighborEdges(sourceView);

                foreach (EdgeInfo edge in edgeList)
                {
                    // if it is a forward edge follow it
                    NodeInfo targetView = null;
                    if (down && sourceView == edge.sourceNodeInfo)
                    {
                        targetView = edge.targetNodeInfo;
                    }
                    if (!down && sourceView == edge.targetNodeInfo)
                    {
                        targetView = edge.sourceNodeInfo;
                    }
                    if (targetView != null)
                    {
                        CellWrapper targetWrapper = vertToWrapper[targetView];

                        // do it only if the edge is a forward edge to a deeper level
                        if (down && targetWrapper != null && targetWrapper.getLevel() > levelIndex)
                        {
                            targetWrapper.addToEdgeCrossesIndicator(sourceWrapper.getEdgeCrossesIndicator());
                        }
                        if (!down && targetWrapper != null && targetWrapper.getLevel() < levelIndex)
                        {
                            targetWrapper.addToEdgeCrossesIndicator(sourceWrapper.getEdgeCrossesIndicator());
                        }
                    }
                }
            }
            return movements;
        }

        private void moveToBarycenter(List<List<CellWrapper>> levels, HashSet<NodeInfo> vertexSet)
        {
            foreach (NodeInfo v in vertexSet)
            {
                // BAND-AID for reasons I haven't figured out yet.
                // Skip this node if it's not in the mapping for whatever reason.
                // The original code assumes it will be in the mapping and doesn't check. 
                // Replicate error using "htop" Ubuntu 20.04 elf binary.
                if (!vertToWrapper.ContainsKey(v)) continue;

                CellWrapper currentwrapper = vertToWrapper[v];

                List<EdgeInfo> edgeList = getNeighborEdges(v);

                foreach (EdgeInfo edge in edgeList)
                {
                    // i have to find neigbhor vertex
                    NodeInfo neighborVertex = null;

                    if (v == edge.sourceNodeInfo)
                    {
                        neighborVertex = edge.targetNodeInfo;
                    }
                    else
                    {
                        if (v == edge.targetNodeInfo)
                        {
                            neighborVertex = edge.sourceNodeInfo;
                        }
                    }

                    if ((neighborVertex != null) && (neighborVertex != v))
                    {
                        CellWrapper neighborWrapper = vertToWrapper[neighborVertex];

                        if (!(currentwrapper == null || neighborWrapper == null || currentwrapper.level == neighborWrapper.level))
                        {
                            currentwrapper.priority++;
                        }
                    }
                }
            }
            foreach (List<CellWrapper> level in levels)
            {
                int pos = 0;
                foreach (CellWrapper wrapper in level)
                {
                    // calculate the initial Grid Positions 1, 2, 3, .... per Level
                    wrapper.setGridPosition(pos++);
                }
            }

            int movementsCurrentLoop = -1;

            while (movementsCurrentLoop != 0)
            {
                // reset movements
                movementsCurrentLoop = 0;

                // top down
                for (int i = 1; i < levels.Count; i++)
                {
                    movementsCurrentLoop += moveToBarycenter(levels, i);
                }
                // bottom up
                for (int i = levels.Count - 1; i >= 0; i--)
                {
                    movementsCurrentLoop += moveToBarycenter(levels, i);
                }
            }
        }

        private List<EdgeInfo> getNeighborEdges(NodeInfo v) 
        {
            List<EdgeInfo> edgeList = new List<EdgeInfo>();
            edgeList.AddRange(v.targetEdgeInfos);
            edgeList.AddRange(v.sourceEdgeInfos);
            return edgeList;
        }

        private int moveToBarycenter(List<List<CellWrapper>> levels, int levelIndex)
        {
            // Counter for the movements
            int movements = 0;

            // Get the current level
            List<CellWrapper> currentLevel = levels[levelIndex];

            for (int currentIndexInTheLevel = 0; currentIndexInTheLevel < currentLevel.Count; currentIndexInTheLevel++)
            {
                CellWrapper sourceWrapper = currentLevel[currentIndexInTheLevel];

                float gridPositionsSum = 0;
                float countNodes = 0;

                NodeInfo vertexView = sourceWrapper.getVertexView();

                List<EdgeInfo> edgeList = getNeighborEdges(vertexView);

                foreach (EdgeInfo edge in edgeList)
                {
                    // if it is a forward edge follow it
                    NodeInfo neighborVertex = null;
                    if (vertexView == edge.sourceNodeInfo)
                    {
                        neighborVertex = edge.targetNodeInfo;
                    }
                    else
                    {
                        if (vertexView == edge.targetNodeInfo) 
                        {
                            neighborVertex = edge.sourceNodeInfo; 
                        }
                    }

                    if (neighborVertex != null)
                    {
                        CellWrapper targetWrapper = vertToWrapper[neighborVertex];

                        if (!(targetWrapper == sourceWrapper) || (targetWrapper == null || targetWrapper.getLevel() == levelIndex))
                        {
                            gridPositionsSum += targetWrapper.getGridPosition();
                            countNodes++;
                        }
                    }
                }

                if (countNodes > 0)
                {
                    float tmp = (gridPositionsSum / countNodes);
                    int newGridPosition = (int)Math.Round(tmp);
                    bool toRight = (newGridPosition > sourceWrapper.getGridPosition());

                    bool moved = true;

                    while (newGridPosition != sourceWrapper.getGridPosition() && moved)
                    {
                        moved = move(toRight, currentLevel, currentIndexInTheLevel, sourceWrapper.getPriority());
                        if (moved)
                        {
                            movements++;
                        }
                    }
                }
            }
            return movements;
        }

        /**@param toRight <tt>true</tt> = try to move the currentWrapper to right; <tt>false</tt> = try to move the currentWrapper to left;
        * @param currentLevel List which contains the CellWrappers for the current level
        * @param currentIndexInTheLevel
        * @param currentPriority
        * @param currentWrapper The Wrapper
        * @return The free GridPosition or -1 is position is not free.
        */
        private bool move(bool toRight, List<CellWrapper> currentLevel, int currentIndexInTheLevel, int currentPriority)
        {
            CellWrapper currentWrapper = currentLevel[currentIndexInTheLevel];

            bool moved = false;
            int neighborIndexInTheLevel = currentIndexInTheLevel + (toRight ? 1 : -1);
            int newGridPosition = currentWrapper.getGridPosition() + (toRight ? 1 : -1);

            if (0 > newGridPosition || newGridPosition >= gridAreaSize)
            {
                return false;
            }

            // if the node is the first or the last we can move
            if (toRight && currentIndexInTheLevel == currentLevel.Count - 1 || !toRight && currentIndexInTheLevel == 0)
            {
                moved = true;
            }
            else
            {
                // else get the neighbor and ask his gridposition
                // if he has the requested new grid position
                // check the priority
                CellWrapper neighborWrapper = currentLevel[neighborIndexInTheLevel];

                int neighborPriority = neighborWrapper.getPriority();

                if (neighborWrapper.getGridPosition() == newGridPosition)
                {
                    if (neighborPriority >= currentPriority)
                    {
                        return false;
                    }
                    else
                    {
                        moved = move(toRight, currentLevel, neighborIndexInTheLevel, currentPriority);
                    }
                }
                else
                {
                    moved = true;
                }
            }

            if (moved)
            {
                currentWrapper.setGridPosition(newGridPosition);
            }
            return moved;
        }

        public void reset()
        {
            traversalSet.Clear();
            vertToWrapper.Clear();
            executed = false;
        }


        class CellWrapper : IComparable<CellWrapper>
        {
            // sum value for edge Crosses
            private double edgeCrossesIndicator = 0;

            // counter for additions to the edgeCrossesIndicator
            private int additions = 0;

            // the vertical level where the cell wrapper is inserted
            public int level = 0;

            // current position in the grid
            public int gridPosition = 0;

            // priority for movements to the barycenter
            public int priority = 0;

            // reference to the node
            private NodeInfo nodeInfo = null;

            // CellWrapper constructor
            public CellWrapper(int level, double edgeCrossesIndicator, NodeInfo nodeInfo)
            {
                this.level = level;
                this.edgeCrossesIndicator = edgeCrossesIndicator;
                this.nodeInfo = nodeInfo;
                additions++;
            }

            // returns the wrapped Vertex
            public NodeInfo getVertexView()
            {
                return nodeInfo;
            }

            // returns the average value for the edge crosses indicator for the wrapped cell
            public double getEdgeCrossesIndicator()
            {
                if (additions == 0)
                    return 0;
                return edgeCrossesIndicator / additions;
            }

            // Adds a value to the edge crosses indicator for the wrapped cell
            public void addToEdgeCrossesIndicator(double addValue)
            {
                edgeCrossesIndicator += addValue;
                additions++;
            }

            // gets the level of the wrapped cell
            public int getLevel()
            {
                return level;
            }

            // gets the grid position for the wrapped cell
            public int getGridPosition()
            {
                return gridPosition;
            }

            // Sets the grid position for the wrapped cell
            public void setGridPosition(int pos)
            {
                this.gridPosition = pos;
            }

            // increments the the priority of this cell wrapper.
            // The priority was used by moving the cell to its barycenter.
            void incrementPriority()
            {
                priority++;
            }

            // returns the priority of this cell wrapper.
            // The priority was used by moving the cell to its barycenter.
            public int getPriority()
            {
                return priority;
            }

            // @see java.lang.Comparable#compareTo(Object)
            public int CompareTo(CellWrapper compare)
            {
                if (compare.getEdgeCrossesIndicator() == this.getEdgeCrossesIndicator())
                    return 0;

                double compareValue = compare.getEdgeCrossesIndicator() - this.getEdgeCrossesIndicator();

                return (int) (compareValue * 1000);

            }
        }

        enum Orientation
        {
            TOP, LEFT
        }
    }
}


