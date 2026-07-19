"use strict";

(function (global) {
    let activeEscapeCancel = null;

    function InlineEditController(options) {
        options = options || {};
        this.originalValue = options.value == null ? "" : options.value;
        this.value = this.originalValue;
        this.onSave = options.onSave || function () { return Promise.resolve(); };
        this.onError = options.onError || function () {};
        this.editing = false;
        this.pending = false;
        this.commitInFlight = false;
        this.suppressNextBlur = false;
    }

    InlineEditController.prototype.begin = function () {
        if (this.pending) {
            return false;
        }
        this.value = this.originalValue;
        this.editing = true;
        return true;
    };

    InlineEditController.prototype.cancel = function () {
        this.value = this.originalValue;
        this.editing = false;
        this.suppressNextBlur = true;
    };

    InlineEditController.prototype.handleBlur = function () {
        if (this.suppressNextBlur) {
            this.suppressNextBlur = false;
            return Promise.resolve(false);
        }
        return this.commit();
    };

    InlineEditController.prototype.commit = function () {
        const controller = this;
        if (!controller.editing || controller.pending || controller.commitInFlight) {
            return Promise.resolve(false);
        }
        if (controller.value === controller.originalValue) {
            controller.editing = false;
            return Promise.resolve(false);
        }

        controller.commitInFlight = true;
        controller.pending = true;
        return Promise.resolve(controller.onSave(controller.value, controller.originalValue))
            .then(function () {
                controller.originalValue = controller.value;
                controller.editing = false;
                return true;
            })
            .catch(function (error) {
                controller.value = controller.originalValue;
                controller.editing = false;
                controller.onError(error);
                throw error;
            })
            .finally(function () {
                controller.pending = false;
                controller.commitInFlight = false;
            });
    };

    function createInlineEditComponent() {
        return {
            name: "InlineEdit",
            props: {
                value: { type: [String, Number], default: "" },
                editorType: { type: String, default: "text" },
                options: { type: Array, default: function () { return []; } },
                searchable: { type: Boolean, default: false },
                commitOnChange: { type: Boolean, default: false },
                label: { type: String, required: true },
                editorId: { type: String, default: "" },
                placeholder: { type: String, default: "" },
                disabled: { type: Boolean, default: false },
                formatValue: { type: Function, default: function (value) { return value || ""; } },
                onSave: { type: Function, required: true },
                onError: { type: Function, default: function () {} }
            },
            emits: ["error"],
            setup: function (props, context) {
                const controller = global.Vue.reactive(new InlineEditController({
                    value: props.value,
                    onSave: props.onSave,
                    onError: reportError
                }));
                const filterText = global.Vue.ref("");
                const editor = global.Vue.ref(null);
                const filter = global.Vue.ref(null);
                const composite = global.Vue.ref(null);
                let escapeKeydownListener = null;
                let escapeKeydownTarget = null;

                const displayValue = global.Vue.computed(function () {
                    return props.formatValue(controller.originalValue);
                });
                const filteredOptions = global.Vue.computed(function () {
                    const filterValue = filterText.value.trim().toLowerCase();
                    if (!filterValue) {
                        return props.options;
                    }
                    return props.options.filter(function (option) {
                        return String(option.label || "").toLowerCase().indexOf(filterValue) >= 0 || String(option.value || "").toLowerCase().indexOf(filterValue) >= 0;
                    });
                });
                const selectSize = global.Vue.computed(function () {
                    return Math.max(2, Math.min(filteredOptions.value.length, 8));
                });

                function reportError(error) {
                    props.onError(error);
                    context.emit("error", error);
                }

                function stopEscapeCapture() {
                    if (escapeKeydownTarget) {
                        escapeKeydownTarget.removeEventListener("keydown", escapeKeydownListener, true);
                        escapeKeydownTarget = null;
                    }
                    if (activeEscapeCancel === cancelEdit) {
                        activeEscapeCancel = null;
                    }
                }

                function cancelEdit() {
                    if (!controller.editing) {
                        return;
                    }
                    controller.cancel();
                    stopEscapeCapture();
                }

                function startEscapeCapture() {
                    const target = global.document && typeof global.document.addEventListener === "function" ? global.document : global;
                    if (escapeKeydownTarget || typeof target.addEventListener !== "function") {
                        return;
                    }
                    escapeKeydownListener = function (event) {
                        if (activeEscapeCancel === cancelEdit && event.key === "Escape") {
                            event.preventDefault();
                            event.stopPropagation();
                            cancelEdit();
                        }
                    };
                    escapeKeydownTarget = target;
                    target.addEventListener("keydown", escapeKeydownListener, true);
                }

                function begin() {
                    if (props.disabled || !controller.begin()) {
                        return;
                    }
                    if (activeEscapeCancel && activeEscapeCancel !== cancelEdit) {
                        activeEscapeCancel();
                    }
                    activeEscapeCancel = cancelEdit;
                    filterText.value = "";
                    startEscapeCapture();
                    global.Vue.nextTick(function () {
                        const activeEditor = props.searchable ? filter.value : editor.value;
                        if (activeEditor) {
                            activeEditor.focus();
                        }
                    });
                }

                function commit() {
                    return controller.commit().catch(function () {
                        return false;
                    }).finally(stopEscapeCapture);
                }

                function onKeydown(event) {
                    if (!controller.editing) {
                        return;
                    }
                    if (event.key === "Escape") {
                        event.preventDefault();
                        cancelEdit();
                    } else if (event.key === "Enter" && props.editorType !== "textarea") {
                        event.preventDefault();
                        commit();
                    }
                }

                function onBlur() {
                    controller.handleBlur().catch(function () {
                        return false;
                    }).finally(stopEscapeCapture);
                }

                function onFilterInput(event) {
                    filterText.value = event.target.value;
                }

                function onSelectChange() {
                    if (props.commitOnChange) {
                        commit();
                    }
                }

                function onCompositeFocusout() {
                    global.setTimeout(function () {
                        if (controller.editing && composite.value && !composite.value.contains(global.document.activeElement)) {
                            onBlur();
                        }
                    }, 0);
                }

                global.Vue.watch(function () { return props.value; }, function (value) {
                    if (!controller.editing && !controller.pending) {
                        controller.originalValue = value == null ? "" : value;
                        controller.value = controller.originalValue;
                    }
                });
                global.Vue.onBeforeUnmount(stopEscapeCapture);

                return { controller: controller, filterText: filterText, editor: editor, filter: filter, composite: composite, displayValue: displayValue, filteredOptions: filteredOptions, selectSize: selectSize, begin: begin, commit: commit, cancelEdit: cancelEdit, onKeydown: onKeydown, onBlur: onBlur, onFilterInput: onFilterInput, onSelectChange: onSelectChange, onCompositeFocusout: onCompositeFocusout };
            },
            template: `
                <span class="admin-inline-edit" :class="{ 'admin-inline-edit--pending': controller && controller.pending }">
                    <button v-if="!controller || !controller.editing" type="button" class="admin-inline-edit__display" :disabled="disabled || (controller && controller.pending)" :aria-label="label" @click="begin"><span class="admin-inline-edit__display-label">{{ displayValue || placeholder }}</span><span class="admin-inline-edit__display-chevron" aria-hidden="true"></span></button>
                    <span v-else-if="editorType === 'select' && searchable" ref="composite" class="admin-inline-edit__searchable" @focusout="onCompositeFocusout">
                        <label class="sr-only" :for="(editorId || 'inlineEdit') + '-filter'">Filter {{ label }}</label>
                        <input :id="(editorId || 'inlineEdit') + '-filter'" ref="filter" :value="filterText" type="search" class="admin-inline-edit__input admin-inline-edit__filter" :placeholder="'Filter ' + label" :disabled="controller.pending" @input="onFilterInput" @keydown="onKeydown">
                        <select :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__select" :size="selectSize" :aria-label="label" :disabled="controller.pending" @change="onSelectChange" @keydown="onKeydown">
                            <option v-for="option in filteredOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
                        </select>
                        <span v-if="filteredOptions.length === 0" class="admin-inline-edit__empty" role="status">No matching options.</span>
                    </span>
                    <select v-else-if="editorType === 'select'" :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__select" :size="selectSize" :aria-label="label" :disabled="controller.pending" @change="onSelectChange" @keydown="onKeydown" @blur="onBlur">
                        <option v-for="option in options" :key="option.value" :value="option.value">{{ option.label }}</option>
                    </select>
                    <textarea v-else-if="editorType === 'textarea'" :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__input--textarea" :placeholder="placeholder" :aria-label="label" :disabled="controller.pending" @keydown="onKeydown" @blur="onBlur"></textarea>
                    <input v-else :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input" :type="editorType" :placeholder="placeholder" :aria-label="label" :disabled="controller.pending" @keydown="onKeydown" @blur="onBlur">
                    <span v-if="controller && controller.pending" class="admin-inline-edit__pending" aria-live="polite">Saving</span>
                </span>`
        };
    }

    const component = createInlineEditComponent();
    global.InlineEditController = InlineEditController;
    global.InlineEdit = component;
    if (typeof module !== "undefined" && module.exports) {
        module.exports = { InlineEditController: InlineEditController, InlineEdit: component };
    }
}(typeof window !== "undefined" ? window : global));