using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Represents a plant location on the game board, managing both the physical location
    ///     and its associated card holders. Provides cached access to child components.
    /// </summary>
    [Serializable]
    public class PlantHolder
    {
        [SerializeField] private Transform plantLocation;
        [SerializeField] private List<PlacedCardHolder> placedCardHolders = new();

        /// <summary>
        ///     Default constructor for serialization.
        /// </summary>
        public PlantHolder() { }

        /// <summary>
        ///     Creates a PlantHolder from a Transform, optionally initializing card holders.
        /// </summary>
        public PlantHolder(Transform location, bool initializeCardHolders = false)
        {
            plantLocation = location;
            if (initializeCardHolders)
                InitializeCardHolders();
        }

        /// <summary>
        ///     The transform representing this plant's physical location in the scene.
        /// </summary>
        public Transform Transform => plantLocation;

        /// <summary>
        ///     The position of this plant location in world space.
        /// </summary>
        public Vector3 Position => plantLocation ? plantLocation.position : Vector3.zero;

        /// <summary>
        ///     The rotation of this plant location in world space.
        /// </summary>
        public Quaternion Rotation => plantLocation ? plantLocation.rotation : Quaternion.identity;

        /// <summary>
        ///     The pre-cached list of card holders for this plant location.
        /// </summary>
        public IReadOnlyList<PlacedCardHolder> CardHolders => placedCardHolders;

        /// <summary>
        ///     Initializes the card holder list by discovering child components.
        ///     Called during scene initialization or after hierarchy changes.
        /// </summary>
        public void InitializeCardHolders()
        {
            if (!plantLocation) return;
            placedCardHolders = plantLocation.GetComponentsInChildren<PlacedCardHolder>(true).ToList();
        }

        /// <summary>
        ///     Implicit conversion to Transform for backward compatibility.
        ///     Allows use in contexts expecting Transform (e.g., assignments, comparisons).
        ///     Note: For GetComponent calls, use .Transform explicitly.
        /// </summary>
        public static implicit operator Transform(PlantHolder holder)
        {
            return holder?.plantLocation;
        }

        /// <summary>
        ///     Implicit conversion to bool for null/valid checks.
        ///     Allows patterns like if (plantHolder) and Where(loc => loc).
        /// </summary>
        public static implicit operator bool(PlantHolder holder)
        {
            return holder?.plantLocation;
        }
    }
}