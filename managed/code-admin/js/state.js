"use strict";

(function (global) {
    let state = {
        session: null,
        workspace: null,
        page: null,
        selectedClass: "",
        search: "",
        start: 0,
        pageSize: 200,
        selectedIds: {},
        editor: null
    };

    const listeners = [];

    function get() {
        return state;
    }

    function set(patch) {
        state = Object.assign({}, state, patch || {});
        listeners.forEach(function (listener) {
            listener(state);
        });
    }

    function subscribe(listener) {
        listeners.push(listener);
        return function () {
            const index = listeners.indexOf(listener);
            if (index >= 0) {
                listeners.splice(index, 1);
            }
        };
    }

    function toggleSelected(id, checked) {
        const selectedIds = Object.assign({}, state.selectedIds);
        if (checked) {
            selectedIds[id] = true;
        } else {
            delete selectedIds[id];
        }
        set({ selectedIds: selectedIds });
    }

    function clearSelected() {
        set({ selectedIds: {} });
    }

    global.CodeAdminState = {
        get: get,
        set: set,
        subscribe: subscribe,
        toggleSelected: toggleSelected,
        clearSelected: clearSelected
    };
}(window));
