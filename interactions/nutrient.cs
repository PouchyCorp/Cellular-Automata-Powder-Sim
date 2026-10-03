using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
public interface ILife
{
	public float nutrient { get; set; }
	public float maxNutrient => 10.0f;

	public float wetness { get; set; }

	public float maxWetness => 1.0f;
}

public class GiveNutrientRequest
{
	public int x { get; set; }
	public int y { get; set; }

	public int targetX { get; set; }
	public int targetY { get; set; }
	public float NutrientAmount { get; set; }

	public GiveNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)
	{
		this.x = x;
		this.y = y;
		this.targetX = targetX;
		this.targetY = targetY;
		this.NutrientAmount = nutrientAmount;

		if (NutrientAmount < 0)
		{
			throw new ArgumentException("NutrientAmount not be negative");
		}
	}

	public bool IsValid(int maxX, int maxY)
	{
		return NutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
	}
}

public class TakeNutrientRequest
{
	public int x { get; set; }
	public int y { get; set; }

	public int targetX { get; set; }
	public int targetY { get; set; }
	public float NutrientAmount { get; set; }

	public TakeNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)

	{
		this.x = x;
		this.y = y;
		this.targetX = targetX;
		this.targetY = targetY;
		this.NutrientAmount = nutrientAmount;

		if (NutrientAmount < 0)
		{
			throw new ArgumentException("NutrientAmount must not be negative");
		}
	}

	public bool IsValid(int maxX, int maxY)
	{
		return NutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
	}
}

public class TakeWetnessRequest
{
	public int x { get; set; }
	public int y { get; set; }

	public int targetX { get; set; }
	public int targetY { get; set; }
	public float WetnessAmount { get; set; }

	public TakeWetnessRequest(int x, int y, int targetX, int targetY, float wetnessAmount)

	{
		this.x = x;
		this.y = y;
		this.targetX = targetX;
		this.targetY = targetY;
		this.WetnessAmount = wetnessAmount;
		if (WetnessAmount < 0)
		{
			throw new ArgumentException("WetnessAmount must not be negative");
		}
	}

	public bool IsValid(int maxX, int maxY)
	{
		return WetnessAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
	}
}

public class GiveWetnessRequest
{
	public int x { get; set; }
	public int y { get; set; }

	public int targetX { get; set; }
	public int targetY { get; set; }
	public float WetnessAmount { get; set; }

	

	public GiveWetnessRequest(int x, int y, int targetX, int targetY, float wetnessAmount)
	{
		if (wetnessAmount < 0)
		{
			throw new ArgumentException("WetnessAmount must not be negative");
		}
		this.x = x;
		this.y = y;
		this.targetX = targetX;
		this.targetY = targetY;
		this.WetnessAmount = wetnessAmount;
	}

	public bool IsValid(int maxX, int maxY)
	{
		return WetnessAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
	}
}

// Not a thread safe implementation of the singleton
public class NutrientManager
{
	private HashSet<(int, int)> uniqueNutrientTargets = new HashSet<(int, int)>();
	private HashSet<(int, int)> uniqueWetnessTargets = new HashSet<(int, int)>();
	private Dictionary<(int, int), List<TakeNutrientRequest>> takeNutrientRequests = new Dictionary<(int, int), List<TakeNutrientRequest>>();
	private Dictionary<(int, int), List<TakeWetnessRequest>> takeWetnessRequests = new Dictionary<(int, int), List<TakeWetnessRequest>>();
	private Dictionary<(int, int), List<GiveWetnessRequest>> giveWetnessRequests = new Dictionary<(int, int), List<GiveWetnessRequest>>();
	private Dictionary<(int, int), List<GiveNutrientRequest>> giveNutrientRequests = new Dictionary<(int, int), List<GiveNutrientRequest>>();

	private static NutrientManager instance = null;
	private NutrientManager()
	{
	}
	public static NutrientManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new NutrientManager();
			}
			return instance;
		}
	}

	public void AddTakeNutrientRequest(TakeNutrientRequest request, int maxX, int maxY)
	{
		if (request.IsValid(maxX, maxY))
		{
			if (!takeNutrientRequests.ContainsKey((request.targetX, request.targetY)))
			{
				takeNutrientRequests.Add((request.targetX, request.targetY), new List<TakeNutrientRequest>());
				uniqueNutrientTargets.Add((request.targetX, request.targetY));
			}
			takeNutrientRequests[(request.targetX, request.targetY)].Add(request);
		}
	}

	public void AddGiveNutrientRequest(GiveNutrientRequest request, int maxX, int maxY)
	{
		if (request.IsValid(maxX, maxY))
		{
			if (!giveNutrientRequests.ContainsKey((request.targetX, request.targetY)))
			{
				giveNutrientRequests.Add((request.targetX, request.targetY), new List<GiveNutrientRequest>());
				uniqueNutrientTargets.Add((request.targetX, request.targetY));
			}
			giveNutrientRequests[(request.targetX, request.targetY)].Add(request);
		}
	}

	public void AddGiveWetnessRequest(GiveWetnessRequest request, int maxX, int maxY)
	{
		if (request.IsValid(maxX, maxY))
		{
			if (!giveWetnessRequests.ContainsKey((request.targetX, request.targetY)))
			{
				giveWetnessRequests.Add((request.targetX, request.targetY), new List<GiveWetnessRequest>());
				uniqueWetnessTargets.Add((request.targetX, request.targetY));
			}
			giveWetnessRequests[(request.targetX, request.targetY)].Add(request);
		}
	}

	public void AddTakeWetnessRequest(TakeWetnessRequest request, int maxX, int maxY)
	{
		if (request.IsValid(maxX, maxY))
		{
			if (!takeWetnessRequests.ContainsKey((request.targetX, request.targetY)))
			{
				takeWetnessRequests.Add((request.targetX, request.targetY), new List<TakeWetnessRequest>());
				uniqueWetnessTargets.Add((request.targetX, request.targetY));
			}
			takeWetnessRequests[(request.targetX, request.targetY)].Add(request);
		}
	}

	private static void ProcessIncomingNutrientRequests(
		IReadOnlyList<GiveNutrientRequest> requests,
		Element[,] currentGrid,
		ILife targetElement)
	{
		float remainingCapacity = targetElement.maxNutrient - targetElement.nutrient;
		foreach (var request in requests)
		{
			if (remainingCapacity <= 0)
				break;

			if (currentGrid[request.x, request.y] is not ILife givingElement)
				continue;

			float nutrientToGive = Mathf.Min(
				request.NutrientAmount,
				Mathf.Min(remainingCapacity, givingElement.nutrient)
			);

			if (nutrientToGive <= 0)
				continue;

			givingElement.nutrient -= nutrientToGive;
			targetElement.nutrient += nutrientToGive;
			remainingCapacity -= nutrientToGive;
		}
	}

	private static void ProcessOutgoingNutrientRequests(
		IReadOnlyList<TakeNutrientRequest> requests,
		Element[,] currentGrid,
		ILife targetElement)
	{
		float remainingNutrient = targetElement.nutrient;
		foreach (var request in requests)
		{
			if (remainingNutrient <= 0)
				break;

			if (currentGrid[request.x, request.y] is not ILife takingElement)
				continue;

			float nutrientToTake = Mathf.Min(
				request.NutrientAmount,
				Mathf.Min(remainingNutrient, takingElement.maxNutrient - takingElement.nutrient)
			);

			if (nutrientToTake <= 0)
				continue;

			targetElement.nutrient -= nutrientToTake;
			takingElement.nutrient += nutrientToTake;
			remainingNutrient -= nutrientToTake;
		}
	}

	public void ProcessNutrientRequests(
	Element[,] oldGrid,
	Element[,] currentGrid)
	{
		
		foreach (var position in uniqueNutrientTargets.ToList())
		{
		
			if (currentGrid[position.Item1, position.Item2] is not ILife targetElement)
				continue;

			var giveRequests = giveNutrientRequests.TryGetValue(position, out var giveList)
				? giveList.OrderByDescending(request => request.NutrientAmount).ToList()
				: new List<GiveNutrientRequest>();

			var takeRequests = takeNutrientRequests.TryGetValue(position, out var takeList)
				? takeList.OrderByDescending(request => request.NutrientAmount).ToList()
				: new List<TakeNutrientRequest>();

			float totalIncoming = 0f;
			foreach (var request in giveRequests)
			{
				if (currentGrid[request.x, request.y] is not ILife givingElement)
					continue;
				totalIncoming += Mathf.Min(request.NutrientAmount, givingElement.nutrient);
			}

			float totalOutgoing = 0f;
			foreach (var request in takeRequests)
			{
				if (currentGrid[request.x, request.y] is not ILife takingElement)
					continue;
				totalOutgoing += Mathf.Min(request.NutrientAmount, takingElement.maxNutrient - takingElement.nutrient);
			}

			if (totalIncoming >= totalOutgoing)
			{
				ProcessIncomingNutrientRequests(giveRequests, currentGrid, targetElement);
				ProcessOutgoingNutrientRequests(takeRequests, currentGrid, targetElement);
			}
			else
			{
				ProcessOutgoingNutrientRequests(takeRequests, currentGrid, targetElement);
				ProcessIncomingNutrientRequests(giveRequests, currentGrid, targetElement);
			}
		}

		takeNutrientRequests.Clear();
		giveNutrientRequests.Clear();
		uniqueNutrientTargets.Clear();
	}
	private static void ProcessIncomingWetnessRequests(
		IReadOnlyList<GiveWetnessRequest> requests,
		Element[,] oldGrid,
		ILife targetElement)
	{
		float remainingCapacity = targetElement.maxWetness - targetElement.wetness;
		foreach (var request in requests)
		{
			if (remainingCapacity <= 0)
				break;

			if (oldGrid[request.x, request.y] is not ILife givingElement)
				continue;

			float wetnessToGive = Mathf.Min(
				request.WetnessAmount,
				Mathf.Min(remainingCapacity, givingElement.wetness)
			);

			if (wetnessToGive <= 0)
				continue;


			givingElement.wetness -= wetnessToGive;
			targetElement.wetness += wetnessToGive;
			remainingCapacity -= wetnessToGive;
		}
	}

	private static void ProcessOutgoingWetnessRequests(
		IReadOnlyList<TakeWetnessRequest> requests,
		Element[,] oldGrid,
		ILife targetElement)
	{
		float remainingWetness = targetElement.wetness;
		foreach (var request in requests)
		{
			if (remainingWetness <= 0)
				break;

			if (oldGrid[request.x, request.y] is not ILife takingElement)
				continue;

			float wetnessToTake = Mathf.Min(
				request.WetnessAmount,
				Mathf.Min(remainingWetness, takingElement.maxWetness - takingElement.wetness)
			);

			if (wetnessToTake <= 0)
				continue;

			targetElement.wetness -= wetnessToTake;
			takingElement.wetness += wetnessToTake;
			remainingWetness -= wetnessToTake;
		}
	}

	public void ProcessWetnessRequests(
	Element[,] oldGrid,
	Element[,] currentGrid)
	{
		foreach (var position in uniqueWetnessTargets.ToList())
		{
			if (currentGrid[position.Item1, position.Item2] is not ILife targetElement)
				continue;

			var giveRequests = giveWetnessRequests.TryGetValue(position, out var giveList)
				? giveList.OrderByDescending(request => request.WetnessAmount).ToList()
				: new List<GiveWetnessRequest>();

			var takeRequests = takeWetnessRequests.TryGetValue(position, out var takeList)
				? takeList.OrderByDescending(request => request.WetnessAmount).ToList()
				: new List<TakeWetnessRequest>();

			float totalIncoming = 0f;
			foreach (var request in giveRequests)
			{
				if (oldGrid[request.x, request.y] is not ILife givingElement)
					continue;
				totalIncoming += Mathf.Min(request.WetnessAmount, givingElement.wetness);
			}

			float totalOutgoing = 0f;
			foreach (var request in takeRequests)
			{
				if (oldGrid[request.x, request.y] is not ILife takingElement)
					continue;
				totalOutgoing += Mathf.Min(request.WetnessAmount, takingElement.maxWetness - takingElement.wetness);
			}

			if (totalIncoming >= totalOutgoing)
			{
				ProcessIncomingWetnessRequests(giveRequests, oldGrid, targetElement);
				ProcessOutgoingWetnessRequests(takeRequests, oldGrid, targetElement);
			}
			else
			{
				ProcessOutgoingWetnessRequests(takeRequests, oldGrid, targetElement);
				ProcessIncomingWetnessRequests(giveRequests, oldGrid, targetElement);
			}
		}

		takeWetnessRequests.Clear();
		giveWetnessRequests.Clear();
		uniqueWetnessTargets.Clear();
	}
}
