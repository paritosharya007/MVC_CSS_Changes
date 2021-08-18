var TotalModals = 0;
$(document).on({
    'show.bs.modal': function () {
        var zIndex = 1040 + (10 * $('.modal:visible').length);
        $(this).css('z-index', zIndex);
        var containerId = $(this).attr('id') + '-container';
        var modalBody = $('#' + containerId + ' .modal-body');
        setTimeout(function () {
            $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack');
        }, 0);
    },
    'hidden.bs.modal': function () {
        if ($('.modal:visible').length > 0) {
            setTimeout(function () {
                $(document.body).addClass('modal-open');
            }, 0);
        }
    }
}, '.modal');

function GetModal(width) {
    if (typeof width === "undefined" || width === null) {
        width = "80";
    }

    var i = 1;
    while (i < TotalModals + 1)
        if ($("#modal-" + i).data('bs.modal') && $("#modal-" + i).data('bs.modal')._isShown) {
            i++;
        } else break;
    if (i > TotalModals) {
        TotalModals++;
        var html = '<div class="modal fade in" data-backdrop="static" id="modal-' + i + '"><div class="modal-dialog modal-xl" role="document"> <div class="modal-content" id="modal-' + i + `-container">
                <div class="modal-body"><div id='modal-`+ i + `-content'></div><div class="modal-footer"><button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
      </div></div>
                </div></div> </div>`;
        $('#Global-Dynamic-Modals').append(html);
        $('#modal-' + i).on('hidden.bs.modal', function () {
            $(this).remove();
            TotalModals--;
        });
    }
    $("#modal-" + i + " .modal-lg").css('width', width + '%');
    return "#modal-" + i;
}

function GetLoaderModal(width) {
    var modalSelector = GetModal(width);

    $(modalSelector + "-content").append(`<div style="text-align: center; width:100%;"><div class="lds-dual-ring" style="margin-top:10px;"></div></div>`);
    return modalSelector;
}