namespace RpgBattle;

public class CircularLinkedList<T> : LinkedList<T>
{
    public LinkedListNode<T> NextOf(LinkedListNode<T> node) => node.Next ?? First!;
    public LinkedListNode<T> PreviousOf(LinkedListNode<T> node) => node.Previous ?? Last!;
}
