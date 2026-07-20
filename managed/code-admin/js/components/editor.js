"use strict";

(function (global) {
    const viewModel = global.CodeAdminViewModel;

    global.CodeAdminComponents = global.CodeAdminComponents || {};
    global.CodeAdminComponents.Editor = {
        props: { editor: { type: Object, required: true }, selectedClass: { type: String, required: true }, workspace: { type: Object, required: true } },
        emits: ["cancel", "save", "code-value-input", "refresh-metadata", "update:editor"],
        setup: function (props, context) {
            const codeValue = global.Vue.ref(null);
            const draft = global.Vue.ref({});
            const selectedClassInfo = global.Vue.computed(function () { return viewModel.getSelectedClass(props.workspace, props.selectedClass); });
            const selectedClassLabel = global.Vue.computed(function () { const item = selectedClassInfo.value; return item ? item.codeClassDesc + " (" + item.codeClass + ")" : props.selectedClass; });
            const detailFields = global.Vue.computed(function () { return viewModel.getDetailFields({ fieldMetadata: { [props.selectedClass]: draft.value.fieldMetadata } }, props.selectedClass); });
            function refreshMetadata() { context.emit("refresh-metadata"); }
            function setCodeValueValidity(errorMessage) { if (codeValue.value) { codeValue.value.setCustomValidity(errorMessage || ""); } }
            function onCodeValueInput(event) { event.target.setCustomValidity(""); context.emit("code-value-input", draft.value); }
            global.Vue.watch(function () { return props.editor; }, function (editor) { if (editor !== draft.value) { draft.value = Object.assign({}, editor); } }, { immediate: true });
            global.Vue.watch(function () { return draft.value.codeValueError; }, setCodeValueValidity, { immediate: true });
            global.Vue.watch(draft, function (editor) { context.emit("update:editor", editor); }, { deep: true });
            return { codeValue: codeValue, draft: draft, selectedClassInfo: selectedClassInfo, selectedClassLabel: selectedClassLabel, detailFields: detailFields, refreshMetadata: refreshMetadata, onCodeValueInput: onCodeValueInput };
        },
        template: `
            <section class="editor-panel code-admin-workspace panel panel-default" aria-labelledby="codeValueEditorTitle">
                <div class="panel-heading editor-panel-heading"><div><h3 id="codeValueEditorTitle" class="panel-title">{{ draft.mode === "edit" ? "Edit " + draft.codeValue : "Add code value" }}</h3><p class="editor-class-context">{{ selectedClassLabel }}</p></div><div class="admin-actions admin-actions--end"><button type="button" class="admin-action admin-action--secondary admin-action--sm" @click="$emit('cancel')"><i class="fa fa-arrow-left" aria-hidden="true"></i> Back to list</button></div></div>
                <div class="panel-body"><form id="codeValueEditor" @submit.prevent="$emit('save')"><div class="editor-grid">
                    <div v-if="draft.mode === 'edit'" class="form-group"><label class="control-label">Value</label><p class="form-control-static code-value-static">{{ draft.codeValue }}</p></div>
                    <div v-if="draft.mode === 'edit'" class="form-group"><label class="control-label" for="codeValueRank">Rank</label><input id="codeValueRank" v-model.number="draft.orderBy" class="form-control" type="number" min="1" step="1" :disabled="draft.isProtected || draft.inactive || (draft.status && draft.status !== 'N')"></div>
                    <div v-else class="form-group" :class="{ 'has-error': draft.codeValueError }"><label class="control-label" for="codeValue">Value</label><input id="codeValue" ref="codeValue" v-model="draft.codeValue" class="form-control" pattern="[A-Za-z][A-Za-z0-9_-]*" title="Must begin with a letter and use only letters, numbers, hyphens, and underscores." :aria-invalid="draft.codeValueError ? 'true' : null" :aria-describedby="draft.codeValueError ? 'codeValueError' : null" required autofocus @input="onCodeValueInput" @blur="refreshMetadata"><span v-if="draft.codeValueError" id="codeValueError" class="help-block">{{ draft.codeValueError }}</span></div>
                    <div v-for="field in detailFields" :key="field.key" class="form-group" :class="{ 'editor-field-wide': field.controlType === 'textarea' }">
                        <label v-if="field.controlType !== 'radio'" class="control-label" :for="'detail-' + field.key">{{ field.label }}</label>
                        <textarea v-if="field.controlType === 'textarea'" :id="'detail-' + field.key" v-model="draft[field.key]" class="form-control" rows="4" :required="field.required"></textarea>
                        <fieldset v-else-if="field.controlType === 'radio'" class="code-admin-radio-options" :aria-required="field.required ? 'true' : 'false'"><legend>{{ field.label }}</legend><label v-for="(option, index) in field.options" :key="option.value" class="radio-inline" :for="'detail-' + field.key + '-' + index"><input :id="'detail-' + field.key + '-' + index" v-model="draft[field.key]" type="radio" :name="field.key" :value="option.value" :required="field.required && index === 0"> {{ option.label }}</label></fieldset>
                        <select v-else-if="field.controlType === 'select' || field.controlType === 'multiselect'" :id="'detail-' + field.key" v-model="draft[field.key]" class="form-control" :multiple="field.controlType === 'multiselect'" :required="field.required"><option v-if="field.controlType === 'select'" value=""></option><option v-for="option in field.options" :key="option.value" :value="option.value">{{ option.label }}</option></select>
                        <input v-else :id="'detail-' + field.key" v-model="draft[field.key]" class="form-control" :required="field.required">
                    </div>
                </div><div class="editor-actions admin-actions"><button type="submit" class="admin-action admin-action--primary admin-action--sm"><i class="fa fa-check" aria-hidden="true"></i> Save</button><button type="button" class="admin-action admin-action--secondary admin-action--sm" @click="$emit('cancel')"><i class="fa fa-times" aria-hidden="true"></i> Cancel</button></div></form></div>
            </section>`
    };
}(window));