﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // The block "shadow" outline
    public GameObject blockShadowPrefab;
    GameObject blockShadow;
    // The offset that needs to be added to the bottom left corner to bring it to the center
    Vector3 blockShadowOffset = new Vector3(0.5f, 0.261f, 0.4f);

    // The corner of the empty space that the player is looking at right now, if any
    Vector3 blockPlacementPoint;

    // The block that the player is looking at right now
    BlockData currentSelectedBlock;

    VoxelChunk currentChunk;

    public delegate void BlockPlacementEvent(int blockType);
    public static event BlockPlacementEvent OnBlockPlacement;
    public static event BlockPlacementEvent OnBlockRemoval;

    // Start is called before the first frame update
    void Start()
    {
        blockShadow = Instantiate(blockShadowPrefab);
        blockShadow.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit raycastHit;

        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);

        if (Physics.Raycast(ray, out raycastHit, 4f, LayerMask.GetMask("Blocks")))
        {
            currentChunk = raycastHit.collider.gameObject.GetComponent<VoxelChunk>();

            raycastHit.point = currentChunk.transform.InverseTransformPoint(raycastHit.point);
            Vector3 cornerOfBlock = raycastHit.point - (raycastHit.normal / 2);
            cornerOfBlock = new Vector3(Mathf.Floor(cornerOfBlock.x),
                                              Mathf.Floor(cornerOfBlock.y),
                                              Mathf.Floor(cornerOfBlock.z));

            // Check if there is a block already occupying this position
            // probably would be quicker to not get the chunkscript every update but we don't know if we gonna support multiple chunks

            currentSelectedBlock = currentChunk.GetBlockAt(cornerOfBlock);
            currentSelectedBlock.DrawDebugLines();

            blockPlacementPoint = cornerOfBlock;


            
            // If there is already a block at this point, add on the raycast normal so it pushes the selection box to the next empty space
            // as every block is one unit, this means we can just add on the normal with no changes and it works perfectly :)
            if (currentSelectedBlock.type != 0)
            {
                blockPlacementPoint = raycastHit.point + (raycastHit.normal / 2);
                blockPlacementPoint = new Vector3(Mathf.Floor(blockPlacementPoint.x), Mathf.Floor(blockPlacementPoint.y), Mathf.Floor(blockPlacementPoint.z));
                Debug.DrawLine(cornerOfBlock, blockPlacementPoint, Color.yellow);
            }

            blockShadow.SetActive(true);
            blockShadow.transform.position = currentChunk.transform.TransformPoint(blockPlacementPoint); //+ blockShadowOffset;


            if (Input.GetMouseButtonUp(1))
            {
                PlaceBlock(blockPlacementPoint, currentChunk, 1);
            }
            if (Input.GetMouseButtonUp(0)) DigBlock(currentSelectedBlock, currentChunk);

        } else
        {
            blockShadow.SetActive(false);
        }

    }

    private void OnGUI()
    {
        if (currentChunk == null) return;
        Handles.Label(currentChunk.transform.TransformPoint(new Vector3(currentSelectedBlock.x, currentSelectedBlock.y, currentSelectedBlock.z)), currentSelectedBlock.ToString());
        Handles.Label(currentChunk.transform.TransformPoint(blockPlacementPoint) + new Vector3(0, 0.2f, 0), blockPlacementPoint.ToString());
    }

    private void OnDrawGizmos()
    {
        if (currentChunk == null) return;
        Gizmos.DrawSphere(currentChunk.transform.TransformPoint(blockPlacementPoint), 0.1f);
    }

    void PlaceBlock(Vector3 position, VoxelChunk chunk, int blockType)
    {
        Debug.Log("Adding block at " + position + ", currently: " + chunk.GetBlockAt(position).type);
        // Don't place if there is already a block at this position
        if (chunk.GetBlockAt(position).type != 0) return;

        // Don't place if the new block would collide with the player

        if (Physics.OverlapBox(currentChunk.transform.TransformPoint(position) + new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, LayerMask.GetMask("Player")).Length > 0) return;
        chunk.AddBlock(new BlockData
        {
            x = (int)position.x,
            y = (int)position.y,
            z = (int)position.z,
            type = blockType
        });

        // Pass in the placed block type (AudioManager uses this to determine which placing sound to place)
        OnBlockPlacement(blockType);
        chunk.BuildChunk();
    }

    void DigBlock(BlockData block, VoxelChunk chunk)
    {
        Debug.Log("Removing block " + block.ToString());
        if (block.type == 0) return;
        chunk.RemoveBlockAt(new Vector3(block.x, block.y, block.z));

        // Pass in the destroyed block type (AudioManager uses this to determine which destroying sound to place)
        OnBlockRemoval(block.type);

        chunk.BuildChunk();
    }
}

