const registry = new Map();

export function init(boardElement, dotNetRef) {
    const lists = boardElement.querySelectorAll('.kanban-column-list');
    const instances = [];

    lists.forEach(function (list) {
        instances.push(Sortable.create(list, {
            group: 'kanban-board',
            animation: 150,
            ghostClass: 'kanban-card-ghost',
            forceFallback: true,
            onEnd: function (evt) {
                const taskId = evt.item.getAttribute('data-task-id');
                const targetColumnId = evt.to.getAttribute('data-column-id');
                dotNetRef.invokeMethodAsync('OnCardDropped', taskId, targetColumnId, evt.newIndex);
            }
        }));
    });

    registry.set(boardElement, instances);
}

export function destroy(boardElement) {
    const instances = registry.get(boardElement);
    if (!instances) return;

    instances.forEach(function (s) { s.destroy(); });
    registry.delete(boardElement);
}
