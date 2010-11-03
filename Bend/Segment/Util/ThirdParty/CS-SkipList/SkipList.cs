

using System;
using System.Text;

namespace CodeSource.SkipList
{
	public class SkipList
	{
		/* This "Node" class models a node in the Skip List structure. It holds a pair of (key, value)
		 * and also three pointers towards Node situated to the right, upwards and downwards. */
		private class Node
		{
			public Node(IComparable key, object value)
			{
				this.key = key;
				this.value = value;
				this.right = this.up = this.down = null;
			}

			public Node() : this(null, null) {} 

			public IComparable key;
			public object value;
			public Node right, up, down;
		}

		/* Public constructor of the class. Receives as parameters the maximum number
		 * of paralel lists and the probability. */
		public SkipList(int maxListsCount, double probability)
		{
			/* Store the maximum number of lists and the probability. */
			this.maxListsCount = maxListsCount;
			this.probability = probability;

			/* Initialize the sentinel nodes. We will have for each list a distinct
			 * first sentinel, but the second sentinel will be shared among all lists. */
			this.head = new Node[maxListsCount + 1];
			for (int i = 0; i <= maxListsCount; i++)
				this.head[i] = new Node();
			this.tail = new Node();

			/* Link the first sentinels of the lists one to another. Also link all first
			 * sentinels to the unique second sentinel. */
			for (int i = 0; i <= maxListsCount; i++)
			{
				head[i].right = tail;
				if (i > 0)
				{
					head[i].down = head[i-1];
					head[i-1].up = head[i];
				}
			}

			/* For the beginning we have no additional list, only the bottom list. */
			this.currentListsCount = 0;
		}

		/* This is another public constructor. It creates a skip list with 11 aditional lists
		 * and with a probability of 0.5. */
		public SkipList() : this(11, 0.5) {}



		/* Inserts a pair of (key, value) into the Skip List structure. */
		public void Insert(IComparable key, object value)
		{
			/* When inserting a key into the list, we will start from the top list and 
			 * move right until we find a node that has a greater key than the one we want to insert.
			 * At this moment we move down to the next list and then again right.
			 * 
			 * In this array we store the rightmost node that we reach on each level. We need to
			 * store them because when we will insert the new key in the lists, it will be inserted
			 * after these rightmost nodes. */
			Node[] next = new Node[maxListsCount + 1];

			/* Now we will parse the skip list structure, from the top list, going right and then down
			 * and then right again and then down again and so on. We use a "cursor" variable that will
			 * represent the current node that we reached in the skip list structure. */
			Node cursor = head[currentListsCount];
			for (int i=currentListsCount; i>=0; i--)
			{
				/* If we are not at the topmost list, then we move down with one level. */
				if (i < currentListsCount)
					cursor = cursor.down;

				/* While we do not reach the second sentinel and we do not find a greater 
				 * numeric value than the one we want to insert, keep moving right. */
				while ((cursor.right != tail) && (cursor.right.key.CompareTo(key) < 0))
					cursor = cursor.right;

				/* Store this rightmost reached node on this level. */
				next[i] = cursor;
			}

			/* Here we are on the bottom list, and we test to see if the new value to add
			 * is not already in the skip list. If it already exists, then we just update
			 * the value of the key. */
			if ((next[0].right != tail) && (next[0].right.key.Equals(key)))
			{
				next[0].right.value = value;
			}

				/* If the number to insert is not in the list, then we flip the coin and insert
				 * it in the lists. */
			else
			{
				/* We find a new level number which will tell us in how many lists to add the new number. 
				 * This new random level number is generated by flipping the coin (see below). */
				int newLevel = NewRandomLevel();

				/* If the new level is greater than the current number of lists, then we extend our
				 * "rightmost nodes" array to include more lists. In the same time we increase the
				 * number of current lists. */
				if (newLevel > currentListsCount)
				{
					for (int i = currentListsCount + 1; i <= newLevel; i++)
						next[i] = head[i];
					currentListsCount = newLevel;
				}

				/* Now we add the node to the lists and adjust the pointers accordingly. 
				 * We add the node starting from the bottom list and moving up to the next lists.
				 * When we get above the bottom list, we start updating the "up" and "down" pointer of the 
				 * nodes. */
				Node prevNode = null;
				Node n = null;
				for (int i = 0; i <= newLevel; i++)
				{
					prevNode = n;
					n = new Node(key, value);
					n.right = next[i].right;
					next[i].right = n;
					if (i > 0)
					{
						n.down = prevNode;
						prevNode.up = n;
					}
				}
			}
		}

		/* This method computes a random value, smaller than the maximum admitted lists count. This random
		 * value will tell us into how many lists to insert a new key. */
		private int NewRandomLevel()
		{
			int newLevel = 0;
			while ((newLevel < maxListsCount) && FlipCoin())
				newLevel++;
			return newLevel;
		}

		/* This method simulates the flipping of a coin. It returns true, or false, similar to a coint which
		 * falls on one face or the other. */
		private bool FlipCoin()
		{
			double d = r.NextDouble();
			return (d < this.probability);
		}


		/* This method removes a key from the Skip List structure. */
		public void Remove(IComparable key)
		{
			/* For removal too we will search for the element to remove. While searching 
			 * the element, we parse each list, starting from the top list and going down
			 * towards the bottom list. On each list, we move from the first sentinel node
			 * rightwards until either we reach the second sentinel node, either we find 
			 * a node with a key greater or equal than the key we want to remove.
			 * 
			 * For each list we remember the rightmost node that we reached. */
			Node[] next = new Node[maxListsCount + 1];

			/* We search for the value to remove. We start from the topmost list, from the first 
			 * sentinel node and move right, down, right, down, etc.
			 * As we said above, we will remember for each
			 * level the rightmost node that we reach. */
			Node cursor = head[currentListsCount];
			for (int i = currentListsCount; i >= 0; i--)
			{
				/* If we are not on the top level, then we move down one level. */
				if (i < currentListsCount)
					cursor = cursor.down;

				/* Move right as long as we encounter values smaller than the value we want to
				 * remove. */
				while ((cursor.right != tail) && (cursor.right.key.CompareTo(key) < 0))
					cursor = cursor.right;

				/* When we got here, either we reached the second sentinel node on the current
				 * level, either we found a node that is not smaller than the value we want to
				 * remove. It is possible that the node we found is equal to the value that we
				 * want to remove, or it can be greater. In both cases we will store this
				 * rightmost node. */
				next[i] = cursor;
			}

			/* When we got here, we parsed even the bottom list and we stopped before a node
			 * that is greater or equal with the value to remove. We test to see if it is equal
			 * with the value or not. If it is equal, then we remove the value from the bottom
			 * list and also from the lists above. */
			if ((next[0].right != tail) && (next[0].right.key.Equals(key)))
			{
				/* Parse each existing list. */
				for (int i=currentListsCount; i>=0; i--)
				{
					/* And if the rightmost reached node is followed by the key to remove, then
					 * remove the key from the list. */
					if ((next[i].right != tail) && next[i].right.key.Equals(key))
						next[i].right = next[i].right.right;
				}
			}
		}


		/* Finds a key in a Skip List structure. Returns the object associated with that key, or null
		 * if the key is not found. */
		public object Find(IComparable key)
		{
			/* We parse the skip list structure starting from the topmost list, from the first sentinel
			 * node. As long as we have keys smaller than the key we search for, we keep moving right.
			 * When we find a key that is greater or equal that the key we search, then we go down one
			 * level and there we try to go again right. When we reach the bottom list, we stop. */
			Node cursor = head[currentListsCount];
			for (int i = currentListsCount; i >= 0; i--)
			{
				if (i < currentListsCount)
					cursor = cursor.down;

				while ((cursor.right != tail) && (cursor.right.key.CompareTo(key) < 0))
					cursor = cursor.right;
			}

			/* Here we are on the bottom list. Now we see if the searched key is there or not.
			 * If it is, we return the value associated with it. If not, we return null. */
			if ((cursor.right != tail) && (cursor.right.key.Equals(key)))
				return cursor.right.value;
			else
				return null;
		}


		/* This method prints the content of the Skip List structure. It can be useful for debugging. */
		override public string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("current number of lists: "+currentListsCount);
			for (int i=currentListsCount; i>=0; i--)
			{
				sb.Append(Environment.NewLine);
				Node cursor = head[i];
				while (cursor != null)
				{
					sb.Append("[");
					if (cursor.key != null)
						sb.Append(cursor.key.ToString());
					else
						sb.Append("N/A");
					sb.Append(", ");
					if (cursor.value != null)
						sb.Append(cursor.value.ToString());
					else
						sb.Append("N/A");
					sb.Append("] ");
					cursor = cursor.right;
				}
			}
			sb.Append(Environment.NewLine);
			sb.Append("--------------");
			return sb.ToString();
		}


		/* This array will hold the first sentinel node from each list. */
		private Node[] head;

		/* This node will represent the second sentinel for the lists. It is enough
		 * to store only one sentinel node and link all lists to it, instead of creating
		 * sentinel nodes for each list separately. */
		private Node tail;

		/* This number represents the maximum number of lists that can be created. 
		 * However it is possible that not all lists are created, depending on how
		 * the coin flips. In order to optimize the operations we will not create all
		 * lists from the beginning, but will create them only as necessary. */
		private int maxListsCount;

		/* This number represents the number of currently created lists. It can be smaller
		 * than the number of maximum accepted lists. */
		private int currentListsCount;

		/* This number represents the probability of adding a new element to another list above
		 * the bottom list. Usually this probability is 0.5 and is equivalent to the probability
		 * of flipping a coin (that is why we say that we flip a coin and then decide if we
		 * add the element to another list). However it is better to leave this probability
		 * easy to change, because in some situations a smaller or a greater probability can
		 * be better suited. */
		private double probability;

		/* This is a random number generator that is used for simulating the flipping of the coin. */
		private Random r = new Random();
	}
}
